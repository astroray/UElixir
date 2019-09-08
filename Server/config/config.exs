use Mix.Config

config :logger,
  backends: [:console],
  compile_time_purge_level: :info

config :uelixir, UElixir.Database,
  database: "uelixir_database",
  username: "root",
  password: "password",
  hostname: "localhost"

config :uelixir, ecto_repos: [UElixir.Database]
