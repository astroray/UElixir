defmodule UElixir do
  require Logger

  alias UElixir.Channel

  use GenServer

  # Client API
  def start_link(args) do
    GenServer.start_link(__MODULE__, args, name: __MODULE__)
  end

  @spec time_step :: integer
  def time_step() do
    GenServer.call(__MODULE__, :get_time_step)
  end

  @spec current_tick :: integer
  def current_tick() do
    GenServer.call(__MODULE__, :get_current_tick)
  end

  @spec get_channel(integer) :: pid()
  def get_channel(channel_index) do
    GenServer.call(__MODULE__, {:get_channel, channel_index})
  end

  # Server API
  def init(port: port, time_step: time_step, channel_count: channel_count) do
    channel_list =
      Enum.reduce(1..channel_count, %{}, fn channel_index, acc ->
        {:ok, pid} = Channel.start_link(%{time_step: time_step, entity_states: %{}, user_list: %{}})
        Map.put_new(acc, channel_index, pid)
      end)

    :ranch.start_listener(make_ref(), :ranch_tcp, [port: port], UElixir.Listener, [])

    start_tick(time_step)
    {:ok, %{tick: 0, time_step: time_step, channel_list: channel_list}}
  end

  defp start_tick(time_step) do
    Process.send_after(self(), {:tick, time_step}, time_step)
  end

  def handle_info({:tick, time_step}, %{tick: tick, channel_list: channel_list}) do
    start_tick(time_step)
    {:noreply, %{tick: tick + 1, channel_list: channel_list}}
  end

  def handle_call(:get_time_step, _from, state = %{time_step: time_step}) do
    {:reply, time_step, state}
  end

  def handle_call({:get_channel, channel_index}, _from, state = %{channel_list: channel_list}) do
    {:reply, Map.get(channel_list, channel_index), state}
  end

  def handle_call(:get_current_tick, _from, state = %{tick: tick}) do
    {:reply, tick, state}
  end
end
