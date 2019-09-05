defmodule UElixir.Database do
  use Ecto.Repo,
    otp_app: :uelixir,
    adapter: Ecto.Adapters.MyXQL
end
