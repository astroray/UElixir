defmodule UElixir do
  use GenServer

  def start_link(transport_opts) do
    GenServer.start_link(__MODULE__, transport_opts)
  end

  def init(transport_opts) do
    ref = make_ref()
    :ranch.start_listener(ref, :ranch_tcp, transport_opts, UElixir.Dispatcher, [])
  end
end
