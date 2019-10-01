defmodule UElixir.ChannelSupervisor do
  alias UElixir.Channel

  use Supervisor

  def start_link(init_arg \\ %{time_step: 100, channel_count: 1}) do
    Supervisor.start_link(__MODULE__, init_arg, name: __MODULE__)
  end

  def init([time_step: time_step, channel_count: channel_count]) do
    children =
      Enum.map(1..channel_count, fn _channel_index ->
        {Channel, [time_step: time_step]}
      end)

    Supervisor.init(children, strategy: :one_for_one)
  end
end
