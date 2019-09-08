defmodule UElixir.Application do
  use Application

  def start(_type, _args) do
    import Supervisor.Spec

    port = System.get_env("port", "4000") |> String.to_integer()
    tick_resolution = System.get_env("tick", "10") |> String.to_integer()

    children = [
      {UElixir, [port: port, tick_resolution: tick_resolution, channel_count: 1]},
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
