defmodule UElixir.Application do
  use Application

  def start(_type, _args) do
    port = System.get_env("port", "4000") |> String.to_integer()
    time_step = System.get_env("time_step", "100") |> String.to_integer()

    children = [
      {UElixir, [port: port, time_step: time_step, channel_count: 1]},
      UElixir.Database,
      UElixir.Authentication,
    ]

    opts = [strategy: :one_for_one, name: UElixir.Supervisor]
    Supervisor.start_link(children, opts)
  end

  def stop(_) do
    :ok
  end
end
