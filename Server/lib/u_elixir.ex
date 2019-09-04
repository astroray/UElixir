defmodule UElixir do
  use GenServer

  def start_link(transport_opts) do
    GenServer.start_link(__MODULE__, transport_opts)
  end

  def init(transport_opts) do
    ref = make_ref()
    :ranch.start_listener(ref, :ranch_tcp, transport_opts, UElixir.Dispatcher, [])
  end

  # # Client API
  # def accept(port) do
  #   # The options below mean:
  #   #
  #   # 1. `:binary` - receives data as binaries (instead of lists)
  #   # 2. `packet: :line` - receives data line by line
  #   # 3. `active: false` - blocks on `:gen_tcp.recv/2` until data is available
  #   # 4. `reuseaddr: true` - allows us to reuse the address if the listener crashes
  #   #
  #   {:ok, socket} =
  #     :gen_tcp.listen(port, [
  #       :binary,
  #       packet: :line,
  #       active: false,
  #       reuseaddr: true,
  #       nodelay: true
  #     ])

  #   Logger.info("Accepting connections on port #{port}")
  #   loop_acceptor(socket)
  # end

  # defp loop_acceptor(socket) do
  #   {:ok, client} = :gen_tcp.accept(socket)
  #   {:ok, pid} = Task.Supervisor.start_child(UElixir.TaskSupervisor, fn -> serve(client) end)

  #   case :gen_tcp.controlling_process(client, pid) do
  #     :ok -> Logger.info("Connection started : #{inspect(pid)}")
  #   end

  #   loop_acceptor(socket)
  # end

  # defp serve(socket) do
  #   socket
  #   |> read_packet
  #   |> process_command(socket)
  # end

  # defp read_packet(socket) do
  #   :gen_tcp.recv(socket, 0)
  # end

  # defp write_packet(line, socket) do
  #   Logger.info("Writes data : #{line}")
  #   :gen_tcp.send(socket, line)
  # end

  # defp process_command({:error, reason}, _socket) do
  #   case reason do
  #     :closed -> Logger.info("Socket closed.")
  #     _ -> Logger.info("Error : #{to_string(reason)}")
  #   end
  # end

  # defp process_command({:ok, command}, socket) do
  #   Logger.info("Data received : #{command}")

  #   case UElixir.Command.parse(command) do
  #     {:ok, {:echo, message}} -> echo(message, socket)
  #     {:error, _} -> Logger.info("Invalid command : #{command}")
  #   end

  #   serve(socket)
  # end

  # defp echo(message, socket) do
  #   write_packet("#{message}\n", socket)
  # end
end
