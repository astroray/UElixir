# UElixir

Technical demo of MMO Game using Unity3D with dedicated server using Elixir.

Hope that inspire someone trying to use Elixir Server and Unity3D client.

![uelixir_main.gif](/Docs/uelixir_main.gif)

**! Disclaimer** This is my first time to build networking and elixir. So explanation may be inaccurate or inappropriate.

## Capabilities

1. Simple authentication using MySQL.

![uelixir_authentication.gif](/Docs/uelixir_authentication.gif)

2. Replicates component's property.

```cs
public sealed class NetworkTransform : NetworkComponent
{
    [SerializeField]
    private float m_positionThreshold = 0.01f;
    [SerializeField]
    private float m_rotationThreshold = 0.01f;

    [Replicable, JsonConverter(typeof(VectorConverter))]
    public Vector3 Position { get => transform.position; set => transform.position = value; }
    [Replicable, JsonConverter(typeof(QuaternionConverter))]
    public Quaternion Rotation { get => transform.rotation; set => transform.rotation = value; }
```

3. Linear interpolation of network transform.

![uelixir_main.gif](/Docs/uelixir_main.gif)

## Assumptions & Limitations

**Assumes that all client is reliable.**

This is bad assumption. But since this is simple demo project, I didn't implement functionalities such as checking whether the state is valid.

**Scale property of Transform is ignored for simplicity**

I don't care about scale and it is rarely concerned.

**Adds components dynamically is not supported.**

As same as above, it is just for simplicity. To solve this, you need to write custom AddComponent method.
And when a component added and it is NetworkComponent then adds the component to NetworkEntity's component map.

**Entities having local authority will not receive states from the server.**

Needs to add some logic to force update local entities.

## Dependencies

- [Unity3D 2018.3 above](https://www.unity3d.com)
- [Json.Net for Unity](https://assetstore.unity.com/packages/tools/input-management/json-net-for-unity-11347)
- [Elixir v1.9 above](https://elixir-lang.org/install.html)
- [MySQL Community Edition](https://dev.mysql.com/downloads/)

## How to install

1. Clone or download repository https://github.com/Astroray073/UElixir.git
2. Install [Elixir](https://elixir-lang.org/install.html) if you not already install it.
3. Install [MySQL](https://dev.mysql.com/downloads/).

### Run server

#### Server configuration

First, you need to get dependencies for our mix project.

```elixir
# Run this command at ~/UElixir/Server
mix deps.get # Get dependencies for mix project.
```

To connect to SQL database, you need to modify ~UElixir/config/config.ex.

```elixir
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
```

Modify the username and password fields as your database admin account.

#### Adds some accounts in SQL database.

![create_accounts.png](/Docs/create_accounts.png)

#### Run server via following command.

```elixir
mix run --no-halt
```

### Run client

1. Build unity project.
2. Run client.

![client_login.png](/Docs/client_login.png)

3. Login via information your account information.

# How it works?

## Server side

Detailed explanation about this project on server side.

### Dependencies

- [Ranch](https://github.com/ninenines/ranch) : TCP acceptor pool.
- [Jason](https://hexdocs.pm/jason/readme.html) : Fast json serializer.
- [Ecto](https://hexdocs.pm/ecto/Ecto.html) : SQL Database wrapper and query generator.
- [MyXQL](https://hexdocs.pm/myxql/readme.html) : MySQL driver.

### Project structure

- /config/config.ex : mix config file.
- /lib
    - /model : data models.
        - user.ex : schema definition for user account.
    - /protocol : network modules of UElixir.
        - authentication.ex : manages user connection information.
        - channel.ex : manages user groups.
        - listener.ex : handles TCP packet and message.
        - message.ex : represents the message from clients to the server.
        - response.ex : represents the response from the server to clients.
    - application.ex : application entry to supervise other modules.
    - database.ex : [Ecto](https://hexdocs.pm/ecto/Ecto.html) repository to MySQL.
    - u_elixir.ex : represents the server.
- mix.exs : mix project.

### Modules

**application.ex**

UElixir.Application is entry point of this mix project. There are a few options you can adjust.

- port : the port number for your network connection.
- time_step : the internal time to apply authoritative state to your clients in milliseconds.
- channel_count : the number of channels to separate user group. **(NOT_IMPLEMENTED)**

```elixir
defmodule UElixir.Application do
  use Application

  def start(_type, _args) do
    import Supervisor.Spec

    port = System.get_env("port", "4000") |> String.to_integer()
    time_step = System.get_env("time_step", "100") |> String.to_integer()

    children = [
      {UElixir, [port: port, time_step: time_step, channel_count: 1]},
      UElixir.Database,
      UElixir.Authentication,
    ]

    opts = [strategy: :one_for_one, name: UElixir.Supervisor]
    Supervisor.start_link(children, opts)
  end

  def stop(_) do
    :ok
  end
end
```

Supervisor runs children : UElixir, Database, Authentication modules.

**u_elixir.ex**

After UElixir.Application starts, UElixir module will be started.
UElixir module runs channels and its listeners for each socket.

```elixir
def init(port: port, time_step: time_step, channel_count: channel_count) do
    channel_list = Enum.reduce(1..channel_count, %{}, fn channel_index, acc ->
        {:ok, pid} = Channel.start_link(%{time_step: time_step, entity_states: %{}, user_list: %{}})
        Map.put_new(acc, channel_index, pid)
    end)

    :ranch.start_listener(make_ref(), :ranch_tcp, [port: port], UElixir.Listener, [])

    start_tick(time_step)
    {:ok, %{tick: 0, time_step: time_step, channel_list: channel_list}}
end
```

**listener.ex**

This is the key part of TCP communication.

```elixir
defmodule UElixir.Listener do
  require Logger

  use GenServer

  @behaviour :ranch_protocol

  alias UElixir.{Message, Response, Authentication, Channel}

  # Client API
  def start_link(ref, _socket, transport, opts) do
    pid = :proc_lib.spawn_link(__MODULE__, :init, [{ref, transport, opts}])
    {:ok, pid}
  end

  # Server API
  def init({ref, transport, _}) do
    Logger.info("Starts protocol")

    {:ok, socket} = :ranch.handshake(ref)
    :ok = transport.setopts(socket, active: true, nodelay: true)

    :gen_server.enter_loop(__MODULE__, [], %{
      ref: ref,
      socket: socket,
      transport: transport,
      # Channel index where the user is, TODO: Dynamically assign the value
      channel_index: 1
    })
  end
```

If you uses GenServer, you cannot call ```GenServer.start_link/1``` to initialize.
Because that call never return until ```init/1``` returns.
The detailed explanation available [here](https://ninenines.eu/docs/en/ranch/2.0/guide/protocols/).

**Handling Message**

All messages is transported as json string.

```elixir
  # message receive callback
  def handle_info({:tcp, _socket, data}, state) do
    String.split(data, "\n", trim: true)
    |> Enum.each(fn line -> dispatch_message(line, state) end)

    {:noreply, state}
  end
```

UElixir protocol assumes all messages ends with line-ending character ```'\n'```.

Splits line by line to send it over to ```dispatch_message/2```.

```elixir
  # handle all messages
  @spec dispatch_message(String.t(), any()) :: :ok | {:error, any()}
  defp dispatch_message(data, state) do
    case Message.parse(data) do
      {:ok, message} -> handle_message(message.request, message.id, message.arg, state)
      {:error, reason} -> on_error(reason)
    end
  end
```

```dispatch_message/2``` call ```handle_message/4``` callbacks to handle actual message.

Here is simple echo callback of ```handle_message/4```.

```elixir
  # echo callback
  @spec handle_message(atom(), integer, String.t(), any()) :: :ok | {:error, any()}
  defp handle_message(
         :echo,
         _from_user_id,
         argument_string,
         %{socket: socket, transport: transport}
       ) do
    send_message(socket, transport, Response.new(:echo, :ok, argument_string))
  end
```

All handle_message/4 has the same spec as above.

- argument 1 (Atom) : Represents the request from the client.
- argument 2 (Integer) : Tells what client request.
- argument 3 (String) : Varies by requests. In this echo request, this is the same string as client sent.
- argument 4 (Any) : Listener's state.
    - ref : unique reference for this listener's parent.
    - socket : listen socket.
    - transport : As we use TCP, so this is the same as ```:ranch_tcp```.
    - channel_index : What channel which the user is.

**channel.ex**

Channel represents the user group to communicate each other.

Given time step, It broadcasts entity states to all users in this group.

```elixir
  # broadcast entity states
  def handle_info(
        :broadcast_entity_states,
        state = %{time_step: time_step, entity_states: entity_states, user_list: user_list}
      ) do
    argument_string =
      Map.values(entity_states)
      |> Enum.join("\n")

    message = Response.new(:replicate_entity_states, :ok, argument_string)

    Enum.each(user_list, fn {socket, _user_id} ->
      Listener.send_message(socket, :ranch_tcp, message)
    end)

    schedule_broadcast(self(), time_step)
    {:noreply, state}
  end

  defp schedule_broadcast(pid, time_step) do
    Process.send_after(pid, :broadcast_entity_states, time_step)
  end
```

## Client side

Detailed explanation about this project on client side.

### Dependencies

- [JSON .NET For Unity](https://assetstore.unity.com/packages/tools/input-management/json-net-for-unity-11347) : Json .Net porting for Unity3D. Needed for Dictionary serialization.

### Project structure

- /UElixir
    - /Attributes
        - ReplicableAttribute.cs : Marks property as replicable.
    - /Components
        - NetworkComponent.cs : Base class for all network components.
        - NetworkEntity.cs : Marks this game object as network entity.
        - NetworkManager.cs : Manages network resources.
        - NetworkTransform.cs : Replicable transform.
    - /Protocol
        - Authentication.cs : Helper to deal with authentication.
        - Message.cs : Represents the message from clients to server.
        - Response.cs : Represents the message from server to clients.
    - /Serialization
        - JsonSerializer.cs : Wrapper of Json .Net
        - QuaternionConverter.cs : Custom json converter for UnityEngine.Quaternion.
    - AssemblyInfo.cs : Assembly information.

### Components

**NetworkEntity**

NetworkEntity represents unique entity on the network.

![networkentity_property.png](/Docs/networkentity_property.png)

- Has Local Authority : Indicates whether this entity has local authority.
- Network Id : Unique id get from the server.

```cs
    internal NetworkEntityState GetState()
    {
        var entityState = new NetworkEntityState
        {
            EntityId = NetworkId.ToString(),
            ComponentStates = new List<NetworkComponentState>()
        };

        foreach (var networkComponent in m_networkComponents.Values)
        {
            entityState.ComponentStates.Add(networkComponent.GetState());
        }

        return entityState;
    }

    internal void SetState(NetworkEntityState entityState, int timeStamp)
    {
        Assert.AreEqual(NetworkId, new Guid(entityState.EntityId));

        if (HasLocalAuthority)
        {
            return;
        }

        foreach (var componentState in entityState.ComponentStates)
        {
            if (m_networkComponents.TryGetValue(componentState.Name, out var networkComponent))
            {
                networkComponent.SetState(componentState, timeStamp);
            }
            else
            {
                Debug.LogError($"{componentState.Name} doesn't exist.");
            }
        }
    }
```

```NetworkEntity.GetState``` collects all ```NetworkComponent``` states.

```NetworkEntity.SetState``` applies the state from the server.

**NetworkComponent**

This represents the entity state of a single component.

I will explain it with concrete implementation : ```NetworkTransform```

```cs
public sealed class NetworkTransform : NetworkComponent
{
    [SerializeField]
    private float m_positionThreshold = 0.01f;
    [SerializeField]
    private float m_rotationThreshold = 0.01f;

    [Replicable, JsonConverter(typeof(VectorConverter))]
    public Vector3 Position { get => transform.position; set => transform.position = value; }
    [Replicable, JsonConverter(typeof(QuaternionConverter))]
    public Quaternion Rotation { get => transform.rotation; set => transform.rotation = value; }
```

The thresholds prevents high frequency packet sending to reduce the server's burden.

Marking Position and Rotation property as Replicable tells NetworkComponent that these property are important and needs to be sent to server when it should be. ```NetworkComponent``` identifies these properties by using ```Reflection```.

And another important thing to consider when updating transform is the interpolation.

On update new entity state, we need to know the time of the entity state. So ```NetworkEntity.SetState``` has timeStamp argument. The timeStamp argument is an integer value of the tick count of the server when send this state.

```elixir
defmodule UElixir.Response do
  @derive Jason.Encoder
  defstruct [:request, :result, :args, :time_stamp]

  @type t :: %__MODULE__{request: atom(), result: atom(), args: String.t(), time_stamp: integer}

  @spec new(atom(), atom(), String.t()) :: __MODULE__.t()
  def new(request, result, args) do
    %__MODULE__{request: request, result: result, args: args, time_stamp: UElixir.current_tick()}
  end
end
```

When creates new response, the time_stamp is automatically filled with current server's tick count.

Now we can save state by time stamp to do interpolation to show smooth illusion to our client.

```cs
    private IEnumerator UpdateTransform()
    {
        while (true)
        {
            yield return new WaitForFixedUpdate();

            if (Entity.HasLocalAuthority
                || m_nextTimeStamp <= m_prevTimeStamp)
            {
                continue;
            }

            var duration = (m_nextTimeStamp - m_prevTimeStamp) * NetworkManager.Instance.TimeStep;

            if (m_timer > duration)
            {
                Position = m_nextPosition;
                Rotation = m_nextRotation;

                continue;
            }

            var t = m_timer / duration;

            Position = Vector3.Lerp(m_prePosition, m_nextPosition, t);
            Rotation = Quaternion.Lerp(m_prevRotation, m_nextRotation, t);

            m_timer += Time.fixedDeltaTime;
        }
    }
```

The detailed explanation about entity interpolation is [here](https://www.gabrielgambetta.com/entity-interpolation.html).

## References

Useful resources I found when I was working on this project.

- [Official Elixir Document](https://elixir-lang.org/getting-started/introduction.html)
- [Ranch User Guide](https://ninenines.eu/docs/en/ranch/2.0/guide/)
- [Ecto Document](https://hexdocs.pm/ecto/Ecto.html)
- [Jason Document](https://hexdocs.pm/jason/readme.html)
- [Erlang Document](http://erlang.org/doc/man/gen_tcp.html)
- [Fast-Paced Multiplayer (Part III): Entity Interpolation](https://www.gabrielgambetta.com/entity-interpolation.html)