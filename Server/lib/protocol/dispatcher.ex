defmodule UElixir.Dispatcher do
  require Logger

  use GenServer

  @behaviour :ranch_protocol

  alias UElixir.{Message, Authentication}

  # Client API
  def start_link(ref, _socket, transport, opts) do
    pid = :proc_lib.spawn_link(__MODULE__, :init, [{ref, transport, opts}])
    {:ok, pid}
  end

  # Server API
  def init({ref, transport, opts}) do
    Logger.info("Starts protocol")

    {:ok, socket} = :ranch.handshake(ref)
    :ok = transport.setopts(socket, active: true, nodelay: true)
    :gen_server.enter_loop(__MODULE__, opts, %{ref: ref, socket: socket, transport: transport})
  end

  # message receive callback
  def handle_info({:tcp, _socket, data}, state) do
    String.split(data, "\n", trim: true)
    |> Enum.each(fn line -> process_message(line, state) end)

    {:noreply, state}
  end

  # on socket closed
  def handle_info({:tcp_closed, socket}, state = %{socket: socket, transport: transport}) do
    Logger.info("Closing #{inspect(socket)}")
    transport.close(socket)
    {:stop, :normal, state}
  end

  # handles error
  def handle_info({:error, reason}, state) do
    Logger.error("Error : #{inspect(reason)}")
    {:noreply, state}
  end

  # handle all messages
  defp process_message(data, state) do
    case Message.parse(data) do
      {:ok, message} ->
        case message.request do
          :authenticate ->
            {:ok, args} = Jason.decode(message.arg, keys: :atoms)
            authenticate(args, state)

          :echo ->
            GenServer.call(__MODULE__, message)

          _ ->
            GenServer.call(__MODULE__, {:error, message})
        end

      {:error, reason} ->
        GenServer.call(__MODULE__, {:error, reason})
    end
  end

  # echo callback
  def handle_call({:echo, data}, _from, state = %{socket: socket, transport: transport}) do
    send_response(socket, transport, data, state)
  end

  # authenticate callback
  defp authenticate(
         %{user_name: user_name, password: password},
         state = %{socket: socket, transport: transport}
       ) do
    case Authentication.authenticate(user_name, password) do
      {:ok, id} ->
        send_response(socket, transport, %{from: :authenticate, result: :ok, value: to_string(id)}, state)

      {:error, reason} ->
        send_response(socket, transport, %{from: :authenticate, result: :error, value: to_string(reason)}, state)
    end
  end

  # Helper
  def send_response(socket, transport, message, state) do
    {:ok, packet} = Jason.encode(message)

    case transport.send(socket, packet) do
      :ok ->
        Logger.debug("Send response : #{inspect(message)} -> #{inspect(socket)}")
        {:reply, :ok, state}

      {:error, reason} ->
        Logger.debug("Send response error(#{inspect(reason)}), #{packet}")
        {:noreply, reason, state}
    end
  end
end
