defmodule UElixir.Command do
  require Logger

  @doc ~S"""
  Parses the given `json_string` into a command.

  ## Examples

      iex> UElixir.Command.parse(~s({ "user_id": 33, "command": "echo", "args": "message" }))
      {:ok, {:echo, "message"}}

      iex> UElixir.Command.parse(~S|{"user_id":0,"command":"report_unit_state","args":"{\"position\":{\"x\":0.0,\"y\":-0.141264021,\"z\":0.0}}"}|)
      {:ok, {:report_unit_state, 0, %{ position: %{x: 0.0, y: -0.141264021, z: 0.0}}}}

  """
  @spec parse(String.t()) :: {:ok, any()} | {:error, any()}
  def parse(json_string) do
    Logger.debug("Parsing... : #{json_string}")

    case Jason.decode(json_string) do
      {:ok, command_info} ->
        parse(:ok, command_info["user_id"], command_info["command"], command_info["args"])

      {:error, error_type} ->
        parse(:error, error_type, json_string)
    end
  end

  @spec parse(:ok, integer, String.t(), any()) :: {:ok, any()} | {:error, any()}
  defp parse(:ok, user_id, command, args) do
    Logger.debug("user_id: #{user_id}, command: #{command}, args: #{inspect(args)}")

    case command do
      "echo" ->
        {:ok, {:echo, args}}

      "authenticate" ->
        {:ok, {:authenticate}}

      "report_unit_state" ->
        {:ok, transform} = Jason.decode(args, keys: :atoms)
        {:ok, {:report_unit_state, user_id, transform}}

      "get_unit_states" ->
        {:ok, {:get_unit_states}}

      _ ->
        {:error, {:unknown_command, command}}
    end
  end

  @spec parse(:error, any(), String.t()) :: {:error, any()}
  defp parse(:error, _error_type, message) do
    {:error, {:decode_error, "bad json format : #{message}"}}
  end
end
