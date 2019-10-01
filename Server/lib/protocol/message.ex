defmodule UElixir.MessageParsingError do
  defexception [:error_type, :message]

  @type t :: %__MODULE__{error_type: any(), message: String.t()}

  def message(%{error_type: error_type, message: message}) do
    "#{error_type}: #{message}"
  end
end

defmodule UElixir.Message do
  alias UElixir.MessageParsingError

  require Logger

  defstruct [:request, :id, :arg]

  @type t :: %__MODULE__{request: atom(), id: integer, arg: String.t()}

  @doc """
  Parse json string to message.

  ## Examples
    iex> UElixir.Message.parse(~S({"id": -1,"request": "authenticate", "arg": "{user_name: \\\"test_user\\\", password: \\\"test_password\\\"}"}))
    {:ok, %UElixir.Message{id: -1, request: :authenticate, arg: "{user_name: \\\"test_user\\\", password: \\\"test_password\\\"}"}}
  """
  @spec parse(String.t()) :: {:ok, Message.t()} | {:error, MessageParsingError.t()}
  def parse(json) do
    Logger.debug("Parsing... : #{json}")

    case Jason.decode(json, keys: :atoms) do
      {:ok, %{id: id, request: request, arg: arg}} ->
        {:ok, %__MODULE__{id: id, request: String.to_atom(request), arg: arg}}

      {:ok, _} ->
        {:error, %MessageParsingError{error_type: :invalid_match, message: json}}

      {:error, error_type} ->
        {:error, %MessageParsingError{error_type: error_type, message: json}}
    end
  end

  @doc """
  Parse json string to message.
  Raises exception.
  """
  def parse!(json) do
    case parse(json) do
      {:ok, message} -> message
      {:error, error_type} -> raise %MessageParsingError{error_type: error_type, message: json}
    end
  end
end
