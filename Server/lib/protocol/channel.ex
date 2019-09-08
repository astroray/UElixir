defmodule UElixir.Channel do
  use GenServer

  # Client API
  def start_link(default \\ %{}) do
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

  # Server API
  def init(init_arg) do
    {:ok, init_arg}
  end

  def handle_call({:user_exists, socket}, _from, state) do
    {:reply, Map.has_key?(state, socket), state}
  end

  def handle_call({:register_user, socket, user_id}, _from, state) do
    if Map.has_key?(state, socket) do
      {:reply, :error, state}
    else
      {:reply, :ok, Map.put_new(state, socket, user_id)}
    end
  end

  def handle_call({:get_users}, _from, state) do
    {:reply, state, state}
  end

  def handle_cast({:unregister_user, socket}, state) do
    {:noreply, Map.delete(state, socket)}
  end
end
