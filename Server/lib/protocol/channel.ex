defmodule UElixir.Channel do
  alias UElixir.{Response, Listener}
  use GenServer

  # Client API
  def start_link(default \\ %{time_step: 100, entity_states: %{}, user_list: %{}}) do
    GenServer.start_link(__MODULE__, default)
  end

  @doc """
  True if user exists in the world
  """
  @spec user_exists?(pid(), any()) :: true | false
  def user_exists?(pid, socket) do
    GenServer.call(pid, {:user_exists, socket})
  end

  def register_user(pid, socket, id) do
    GenServer.call(pid, {:register_user, socket, id})
  end

  def unregister_user(pid, socket) do
    GenServer.cast(pid, {:unregister_user, socket})
  end

  def get_users(pid) do
    GenServer.call(pid, {:get_users})
  end

  def update_entity_states(pid, socket, entity_state) do
    GenServer.cast(pid, {:update_entity_states, socket, entity_state})
  end

  # Server API
  def init(initial_state = %{time_step: time_step}) do
    schedule_broadcast(self(), time_step)
    {:ok, initial_state}
  end

  def handle_call({:user_exists, socket}, _from, state = %{user_list: user_list}) do
    {:reply, Map.has_key?(user_list, socket), state}
  end

  def handle_call(
        {:register_user, socket, user_id},
        _from,
        state = %{user_list: user_list}
      ) do
    if Map.has_key?(user_list, socket) do
      {:reply, :error, state}
    else
      {:reply, :ok, %{state | user_list: Map.put_new(user_list, socket, user_id)}}
    end
  end

  def handle_call({:get_users}, _from, state = %{user_list: user_list}) do
    {:reply, user_list, state}
  end

  def handle_cast(
        {:unregister_user, socket},
        state = %{entity_states: entity_states, user_list: user_list}
      ) do
    {
      :noreply,
      %{
        state
        | entity_states: Map.delete(entity_states, socket),
          user_list: Map.delete(user_list, socket)
      }
    }
  end

  def handle_cast(
        {:update_entity_states, socket, entity_state},
        state = %{entity_states: entity_states, user_list: user_list}
      ) do
    if Map.has_key?(user_list, socket) do
      {:noreply,
       %{
         state
         | entity_states: Map.put(entity_states, socket, entity_state)
       }}
    else
      {:noreply, state}
    end
  end

  # broadcast entity states
  def handle_info(
        :broadcast_entity_states,
        state = %{time_step: time_step, entity_states: entity_states, user_list: user_list}
      ) do
    argument_string =
      Map.values(entity_states)
      |> Enum.join("\n")

    # argument_string = Enum.reduce(user_list, "", fn {socket, user_id}, acc ->
    #   client_state = %{client_id: user_id, entity_states: Map.get(entity_states, socket)}
    #   {:ok, client_state_string} = Jason.encode(client_state)
    #   acc <> "\n" <> client_state_string
    # end)
    message = Response.new(:replicate_entity_states, :ok, argument_string)

    Enum.each(user_list, fn {socket, _user_id} ->
      Listener.send_message(socket, :ranch_tcp, message)
    end)

    schedule_broadcast(self(), time_step)
    {:noreply, state}
  end

  defp schedule_broadcast(pid, time_step) do
    Process.send_after(pid, :broadcast_entity_states, time_step)
  end
end
