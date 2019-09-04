defmodule UElixir.Vector3 do
  defstruct [:x, :y, :z]

  @type t :: %__MODULE__{x: float, y: float, z: float}

  @doc """
  Create new vector.

  ## Examples

    iex> UElixir.Vector3.new(1.0, 2.0, 3.0)
    %UElixir.Vector3{x: 1.0, y: 2.0, z: 3.0}
  """
  @spec new(float, float, float) :: UElixir.Vector3.t()
  def new(x, y, z) do
    %__MODULE__{x: x, y: y, z: z}
  end

  @doc """
  Create new vector.

  ## Examples

    iex> UElixir.Vector3.new(1.0, 2.0)
    %UElixir.Vector3{x: 1.0, y: 2.0, z: 0.0}
  """
  @spec new(float, float) :: UElixir.Vector3.t()
  def new(x, y) do
    new(x, y, 0.0)
  end

  @doc """
  Create new vector with all component is the given value.

  ## Examples

    iex> UElixir.Vector3.new(1.0)
    %UElixir.Vector3{x: 1.0, y: 1.0, z: 1.0}
  """
  @spec new(float) :: UElixir.Vector3.t()
  def new(value) do
    new(value, value, value)
  end

  @doc """
  Create new vector with all component is zero.

  ## Examples

    iex> UElixir.Vector3.new()
    %UElixir.Vector3{x: 0.0, y: 0.0, z: 0.0}
  """
  @spec new :: UElixir.Vector3.t()
  def new() do
    new(0.0)
  end

  @doc """
  Short handed vector for zero vector.

  ## Examples

    iex> UElixir.Vector3.zero()
    %UElixir.Vector3{x: 0.0, y: 0.0, z: 0.0}
  """
  def zero() do
    new()
  end

  @doc """
  Short handed vector for one vector.

  ## Examples

    iex> UElixir.Vector3.one()
    %UElixir.Vector3{x: 1.0, y: 1.0, z: 1.0}
  """
  def one() do
    new(1.0)
  end
end
