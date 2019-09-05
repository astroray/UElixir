defmodule UElixir.Authentication do
  alias UElixir.Database, as: DB
  alias UElixir.User

  import Ecto.Query

  use GenServer

  # Client API
  def start_link(_default) do
    GenServer.start_link(__MODULE__, [])
  end

  @spec user_name_exists?(String.t()) :: true | false
  def user_name_exists?(user_name) do
    DB.exists?(
      from("users",
        where: [name: ^user_name]
      )
    )
  end

  @spec register_user(String.t(), String.t()) :: {:ok, integer} | {:error, atom}
  def register_user(user_name, password) do
    if user_name_exists?(user_name) do
      {:error, :duplicated_user_name}
    else
      new_user = %User{name: user_name, password: password}
      {:ok, registered_user} = DB.insert(new_user)
      {:ok, registered_user.id}
    end
  end

  @spec authenticate(String.t(), String.t()) :: {:ok, integer} | {:error, atom()}
  def authenticate(user_name, password) do
    if user_name_exists?(user_name) do
      matched =
        DB.one(
          from(User,
            where: [name: ^user_name, password: ^password],
            select: [:id]
          )
        )

      if matched == nil do
        {:error, :password_not_matched}
      else
        {:ok, matched.id}
      end
    else
      {:error, :user_name_not_exists}
    end
  end

  # Server API
  def init(_state) do
    {:ok, nil}
  end
end
