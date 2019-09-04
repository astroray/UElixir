defmodule UElixir.Dispatcher do
  require Logger

  use GenServer

  @behaviour :ranch_protocol

  alias UElixir.{Vector3, Transform}
  alias UElixir.Transform.Repo, as: TransformRepo

  def start_link(ref, _socket, transport, opts) do
    pid = :proc_lib.spawn_link(__MODULE__, :init, [{ref, transport, opts}])
    {:ok, pid}
  end

  def init({ref, transport, opts}) do
    Logger.info("Starts protocol")
    {:ok, socket} = :ranch.handshake(ref)
    :ok = transport.setopts(socket, active: true, nodelay: true)

    :gen_server.enter_loop(__MODULE__, opts, %{ref: ref, socket: socket, transport: transport})
  end

  def handle_info({:tcp, socket, data}, state = %{socket: socket, transport: transport}) do
    String.split(data, "\r\n", trim: true)
    |> Enum.each(fn line -> process_command(socket, transport, line) end)

    {:noreply, state}
  end

  def handle_info({:tcp_closed, socket}, state = %{socket: socket, transport: transport}) do
    Logger.info("Closing #{inspect(socket)}")
    transport.close(socket)
    {:stop, :normal, state}
  end

  defp process_command(socket, transport, message) do
    case UElixir.Command.parse(message) do
      {:ok, {:echo, args}} ->
        echo(socket, transport, args)

      {:ok, {:authenticate}} ->
        authenticate(socket, transport)

      {:ok, {:report_unit_state, user_id, data}} ->
        report_unit_state(user_id, data)

      {:ok, {:get_unit_states}} ->
        get_unit_states(socket, transport)

      {:error, {error_type, command}} ->
        Logger.error("#{error_type} : #{command}")
        send_data(socket, transport, "null\r\n")
    end
  end

  defp send_data(socket, transport, data) do
    case transport.send(socket, data) do
      :ok -> Logger.debug("Send data : #{data} -> #{inspect(socket)}")
      {:error, error_type} -> Logger.error("Failed to send data. Reason : #{inspect(error_type)}")
    end
  end

  # Commands
  def echo(socket, transport, data) do
    send_data(socket, transport, data)
  end

  def authenticate(socket, transport) do
    id = TransformRepo.count()
    TransformRepo.put(id, %Transform{})
    send_data(socket, transport, to_string(id))
  end

  @spec report_unit_state(integer, Transform.t()) :: :ok
  def report_unit_state(user_id, state) do
    TransformRepo.put(user_id, state)

    Logger.debug(inspect(TransformRepo.data()))
  end

  def get_unit_states(socket, transport) do
    unit_states = TransformRepo.data()
    {:ok, json} = Jason.encode(unit_states)
    Logger.debug("Encoded: #{json}")
    send_data(socket, transport, json)
  end
end
