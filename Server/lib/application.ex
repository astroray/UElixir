defmodule UElixir.Application do
  use Application

  def start(_type, _args) do
    import Supervisor.Spec

    port = System.get_env("port", "4000") |> String.to_integer()

    children = [
      UElixir.Database,
      UElixir.Authentication,
      {UElixir, [port: port]}
    ]

    opts = [strategy: :one_for_one, name: UElixir.Supervisor]
    Supervisor.start_link(children, opts)
  end

  def stop(_) do
    :ok
  end
end
