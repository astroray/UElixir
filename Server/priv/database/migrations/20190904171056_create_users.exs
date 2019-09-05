defmodule UElixir.Database.Migrations.CreateUsers do
  use Ecto.Migration

  def change do
    create table(:users) do
      add :name, :string
      add :password, :string
    end
  end
end
