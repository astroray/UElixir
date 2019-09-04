defmodule UElixir.Transform.Repo do
  alias UElixir.{Vector3, Transform}

  use GenServer

  # Client API

  def start_link(default \\ []) when is_list(default) do
    GenServer.start_link(__MODULE__, Map.new(default), name: __MODULE__)
  end

  @doc """
  Get transform of the user.

  ## Examples

    iex> UElixir.Transform.Repo.start_link([])
    ...> UElixir.Transform.Repo.put(42, UElixir.Transform.new())
    ...> UElixir.Transform.Repo.get(42)
    %UElixir.Transform{}
  """
  @spec get(integer) :: Transform.t()
  def get(user_id) do
    GenServer.call(__MODULE__, {:get, user_id})
  end

  @doc """
  Add or update the transform of the user.

  ## Examples

    iex> UElixir.Transform.Repo.start_link([])
    ...> UElixir.Transform.Repo.put(42, UElixir.Transform.new())
    ...> UElixir.Transform.Repo.get(42)
    %UElixir.Transform{}
  """
  def put(user_id, new_transform) do
    GenServer.cast(__MODULE__, {:put, user_id, new_transform})
  end

  @doc """
  Gets current size of repository tuples.

  ## Examples

    iex> UElixir.Transform.Repo.start_link([])
    ...> UElixir.Transform.Repo.put(42, UElixir.Transform.new())
    ...> UElixir.Transform.Repo.put(23, UElixir.Transform.new())
    ...> UElixir.Transform.Repo.count()
    2
    ...> UElixir.Transform.Repo.put(23, UElixir.Transform.new())
    ...> UElixir.Transform.Repo.count()
    2
    ...> UElixir.Transform.Repo.put(72, UElixir.Transform.new())
    ...> UElixir.Transform.Repo.count()
    3
  """
  @spec count :: integer
  def count() do
    GenServer.call(__MODULE__, :count)
  end

  def data() do
    GenServer.call(__MODULE__, :data)
  end

  # Server API
  def init(state), do: {:ok, state}

  def handle_call({:get, user_id}, _from, state) do
    if Map.has_key?(state, user_id) do
      {:reply, Map.get(state, user_id), state}
    else
      {:reply, nil, state}
    end
  end

  def handle_call(:count, _from, state) do
    {:reply, map_size(state), state}
  end

  def handle_call(:data, _from, state) do
    {:reply, state, state}
  end

  def handle_cast({:put, user_id, new_transform}, state) do
    {:noreply, Map.put(state, user_id, new_transform)}
  end

end
