defmodule UElixir.Transform do
  alias UElixir.Vector3

  defstruct position: Vector3.new()

  @type t(position) :: %__MODULE__{position: position}
  @type t :: %__MODULE__{position: Vector3.t()}

  @doc """
  Create new transform

  ## Examples

    iex> UElixir.Transform.new(UElixir.Vector3.new(2.0, 3.0, 4.0))
    %UElixir.Transform{position: %UElixir.Vector3{ x: 2.0, y: 3.0, z: 4.0}}
  """
  @spec new(Vector3.t()) :: __MODULE__.t()
  def new(position) do
    %__MODULE__{position: position}
  end

  @doc """
   Create new transform

  ## Examples

    iex> UElixir.Transform.new()
    %UElixir.Transform{position: %UElixir.Vector3{ x: 0.0, y: 0.0, z: 0.0}}
  """
  @spec new :: __MODULE__.t()
  def new() do
    new(Vector3.new())
  end
end
