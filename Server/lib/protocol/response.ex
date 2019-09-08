defmodule UElixir.Response do
  @derive Jason.Encoder
  defstruct [:request, :result, :args, :time_stamp]

  @type t :: %__MODULE__{request: atom(), result: atom(), args: String.t(), time_stamp: integer}

  @spec new(atom(), atom(), String.t()) :: __MODULE__.t()
  def new(request, result, args) do
    %__MODULE__{request: request, result: result, args: args, time_stamp: UElixir.current_tick()}
  end
end
