defmodule UElixir.Listener do
  require Logger

  use GenServer

  @behaviour :ranch_protocol

  alias UElixir.{Message, Response, Authentication, Channel}

  # Client API
  def start_link(ref, _socket, transport, opts) do
    pid = :proc_lib.spawn_link(__MODULE__, :init, [{ref, transport, opts}])
    {:ok, pid}
  end

  # Server API
  def init({ref, transport, _}) do
    Logger.info("Starts protocol")

    {:ok, socket} = :ranch.handshake(ref)
    :ok = transport.setopts(socket, active: true, nodelay: true)

    :gen_server.enter_loop(__MODULE__, [], %{
      ref: ref,
      socket: socket,
      transport: transport,
      # Channel index where the user is, TODO: Dynamically assign the value
      channel_index: 1
    })
  end

  # message receive callback
  def handle_info({:tcp, _socket, data}, state) do
    String.split(data, "\n", trim: true)
    |> Enum.each(fn line -> dispatch_message(line, state) end)

    {:noreply, state}
  end

  # on socket closed
  def handle_info(
        {:tcp_closed, socket},
        state = %{socket: socket, transport: transport, channel_index: channel_index}
      ) do
    Logger.info("Closing #{inspect(socket)}")
    transport.close(socket)
    UElixir.get_channel(channel_index) |> Channel.unregister_user(socket)
    {:stop, :normal, state}
  end

  # on connection error
  def handle_info({:tcp_error, _, reason}, %{peername: peername} = state) do
    Logger.error(fn ->
      "Error with peer #{peername}: #{inspect(reason)}"
    end)

    {:stop, :normal, state}
  end

  # handle all messages
  @spec dispatch_message(String.t(), any()) :: :ok | {:error, any()}
  defp dispatch_message(data, state) do
    case Message.parse(data) do
      {:ok, message} -> handle_message(message.request, message.id, message.arg, state)
      {:error, reason} -> on_error(reason)
    end
  end

  # handles error
  def on_error(reason) do
    Logger.error("Error : #{inspect(reason)}")
  end

  # echo callback
  @spec handle_message(atom(), integer, String.t(), any()) :: :ok | {:error, any()}
  defp handle_message(
         :echo,
         _from_user_id,
         argument_string,
         %{socket: socket, transport: transport}
       ) do
    send_message(socket, transport, Response.new(:echo, :ok, argument_string))
  end

  # authenticate callback
  defp handle_message(
         :authenticate,
         _from_user_id,
         argument_string,
         %{socket: socket, transport: transport, channel_index: channel_index}
       ) do
    {:ok, %{user_name: user_name, password: password}} =
      Jason.decode(argument_string, keys: :atoms)

    channel_pid = UElixir.get_channel(channel_index)

    case Authentication.authenticate(user_name, password) do
      {:ok, id} ->
        if Channel.user_exists?(channel_pid, socket) do
          send_message(
            socket,
            transport,
            Response.new(:authenticate, :error, "#{user_name} already has been signed in.")
          )
        else
          Channel.register_user(channel_pid, socket, id)
          send_message(socket, transport, Response.new(:authenticate, :ok, to_string(id)))
        end

      {:error, reason} ->
        send_message(socket, transport, Response.new(:authenticate, :error, to_string(reason)))
    end
  end

  # register entity callback
  defp handle_message(
         :register_entity,
         _from,
         _argument_string,
         %{socket: socket, transport: transport}
       ) do
    entity_id = Ecto.UUID.generate()
    send_message(socket, transport, Response.new(:register_entity, :ok, entity_id))
  end

  # update entity states
  defp handle_message(
         :update_entity_states,
         _from_user_id,
         argument_string,
         %{socket: socket, channel_index: channel_index}
       ) do

    UElixir.get_channel(channel_index)
    |> Channel.update_entity_states(socket, argument_string)

    # UElixir.get_channel(channel_index)
    # |> Channel.get_users()
    # |> Enum.each(fn {socket, user_id} ->
    #   if user_id != from_user_id do
    #     send_message(
    #       socket,
    #       transport,
    #       Response.new(:update_entity_states, :ok, argument_string)
    #     )
    #   end
    # end)
  end

  @spec send_message(any(), atom(), Response.t()) :: :ok | {:error, any()}
  def send_message(socket, transport, message) do
    {:ok, packet} = Jason.encode(message)

    case transport.send(socket, "#{packet}\n") do
      :ok ->
        Logger.debug("Send response : #{inspect(message)} -> #{inspect(socket)}")
        :ok

      {:error, reason} ->
        Logger.debug("Send response error(#{inspect(reason)}), #{packet}")
        {:error, reason}
    end
  end
end
