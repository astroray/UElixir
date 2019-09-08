defmodule UElixir do
  require Logger

  alias UElixir.Channel

  use GenServer

  # Client API
  def start_link(args) do
    GenServer.start_link(__MODULE__, args, name: __MODULE__)
  end

  @spec get_channel(integer) :: pid()
  def get_channel(channel_index) do
    GenServer.call(__MODULE__, {:get_channel, channel_index})
  end

  @spec current_time :: integer
  def current_time() do
    GenServer.call(__MODULE__, :current_time)
  end

  # Server API
  def init(port: port, tick_resolution: tick_resoluction, channel_count: channel_count) do
    channel_list =
      Enum.reduce(1..channel_count, %{}, fn channel_index, acc ->
        {:ok, pid} = Channel.start_link()
        Map.put_new(acc, channel_index, pid)
      end)

    :ranch.start_listener(make_ref(), :ranch_tcp, [port: port], UElixir.Listener, [])

    start_tick(tick_resoluction)
    {:ok, %{tick: 0, channel_list: channel_list}}
  end

  defp start_tick(tick_resolution) do
    Process.send_after(self(), {:tick, tick_resolution}, tick_resolution)
  end

  def handle_info({:tick, tick_resolution}, %{tick: tick, channel_list: channel_list}) do
    start_tick(tick_resolution)
    {:noreply, %{tick: tick + tick_resolution, channel_list: channel_list}}
  end

  def handle_call({:get_channel, channel_index}, _from, state = %{channel_list: channel_list}) do
    {:reply, Map.get(channel_list, channel_index), state}
  end

  def handle_call(:current_time, _from, state = %{tick: tick}) do
    {:reply, tick, state}
  end
end
