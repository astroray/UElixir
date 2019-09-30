using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

namespace UElixir
{
    public delegate void ResponseCallback(Response response);

    public delegate void EntitySpawnCallback(NetworkEntity entity, bool isSuccess);

    /// <summary>
    /// Manages network resources such as Socket.
    /// This component should exist only one instance in the scene.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NetworkManager : MonoBehaviour
    {
        private static NetworkManager m_instance;
        public static  NetworkManager Instance => m_instance;

        [SerializeField]
        private string m_hostName = "localhost";
        [SerializeField]
        private int m_port = 4000;
        [SerializeField, Tooltip("Time step of the server in milliseconds.")]
        private int m_timeStep = 100;
        [SerializeField]
        private NetworkEntity m_entityPrefab;

        private const int m_waitInterval = 10;

        private TcpClient m_client = new TcpClient { NoDelay = true };
        public  string    HostName    => m_hostName;
        public  int       Port        => m_port;
        public  bool      IsConnected => m_client.Connected;

        /// <summary>
        /// Time step in seconds.
        /// </summary>
        public float TimeStep => m_timeStep * 0.001f;

        private Thread                            m_listenerThread;
        private ConcurrentQueue<ResponseCallback> m_responseCallbacks = new ConcurrentQueue<ResponseCallback>();
        private Queue<Action>                     m_updateQueue       = new Queue<Action>();
        private Dictionary<Guid, NetworkEntity>   m_entities          = new Dictionary<Guid, NetworkEntity>();

        private void Awake()
        {
            if (Instance == null)
            {
                m_instance = this;
            }
            else if (Instance != this)
            {
                Debug.LogError("NetworkManager should exist only one instance in a scene.");
            }
        }

        private void Update()
        {
            ReportEntityStates();

            while (m_updateQueue.Count > 0)
            {
                m_updateQueue.Dequeue().Invoke();
            }
        }

        private void EnqueueMainThreadCommand(Action command)
        {
            m_updateQueue.Enqueue(command);
        }

        private void OnDestroy()
        {
            if (m_client != null && m_client.Connected)
            {
                m_client.Close();
            }

            if (m_listenerThread != null && m_listenerThread.IsAlive)
            {
                m_listenerThread.Abort();
            }
        }

        /// <summary>
        /// Connects to server asynchronously.
        /// </summary>
        /// <param name="onConnect">Callback on task is completed.</param>
        /// <returns></returns>
        public async Task ConnectAsync(Action<bool> onConnect)
        {
            try
            {
                var address = Dns.GetHostEntry(HostName)
                                 .AddressList
                                 .First(ip => ip.AddressFamily == AddressFamily.InterNetwork);

                await m_client.ConnectAsync(address, Port);

                if (IsConnected)
                {
                    StartListenToServer();
                }
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }

            onConnect?.Invoke(IsConnected);
        }

        private void StartListenToServer()
        {
            m_listenerThread?.Abort();

            m_listenerThread = new Thread(ListenToServer)
            {
                IsBackground = true,
            };

            m_listenerThread.Start();
        }

        [WorkerThread]
        private void ListenToServer()
        {
            using (var stream = m_client.GetStream())
            {
                while (m_client.Connected)
                {
                    while (!stream.DataAvailable)
                    {
                        Thread.Sleep(m_waitInterval);

                        if (!m_client.Connected)
                        {
                            return;
                        }
                    }

                    using (var memoryStream = new MemoryStream())
                    {
                        var buffer = new byte[1024];

                        do
                        {
                            int length = stream.Read(buffer, 0, buffer.Length);
                            memoryStream.Write(buffer, 0, length);
                        } while (stream.DataAvailable);

                        var responseString = Encoding.UTF8.GetString(memoryStream.ToArray()).Trim();

                        foreach (var line in responseString.Split('\n'))
                        {
                            HandleMessage(line);
                        }
                    }
                }
            }
        }

        [WorkerThread]
        private void HandleMessage(string line)
        {
            Debug.Log($"Got response : {line}");
            var response = JsonSerializer.Deserialize<Response>(line);

            if (response.Request.Equals("replicate_entity_states"))
            {
                ReplicateEntityStates(response);

                return;
            }

            if (m_responseCallbacks.Count > 0
                && m_responseCallbacks.TryDequeue(out var callback))
            {
                EnqueueMainThreadCommand(() => { callback.Invoke(response); });
            }
        }

        /// <summary>
        /// Sends a message to server.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="responseCallback">Callback when got response from server. It can be null.</param>
        public void SendToServer(Message message, ResponseCallback responseCallback)
        {
            Assert.IsTrue(IsConnected);

            var stream = m_client.GetStream();
            Assert.IsTrue(stream.CanWrite);

            var packet = message.ToByteArray();
            stream.Write(packet, 0, packet.Length);

            Debug.Log($"Message was sent : {message}");

            if (responseCallback != null)
            {
                m_responseCallbacks.Enqueue(responseCallback);
            }
        }

        private void ReportEntityStates()
        {
            if (!Authentication.IsAuthenticated
                || m_entities.Count == 0
                || m_entities.All(entity => entity.Value.ShouldUpdate == false))
            {
                return;
            }

            var entityStates = m_entities.Values
                                         .Where(entity => entity.HasLocalAuthority)
                                         .Select(entity => JsonSerializer.Serialize(entity.GetState()));

            Message message = new Message
            {
                UserId  = Authentication.ClientId,
                Request = "update_entity_states",
                Arg     = string.Join("\n", entityStates)
            };

            SendToServer(message, null);
        }

        [WorkerThread]
        private void ReplicateEntityStates(Response response)
        {
            if (!Authentication.IsAuthenticated
                || m_entities.Count == 0
                || string.IsNullOrEmpty(response.Args))
            {
                return;
            }

            var entityStates = response.Args.Split('\n')
                                       .Select(JsonSerializer.Deserialize<NetworkEntityState>);

            EnqueueMainThreadCommand(() =>
            {
                var usedEntities = new List<Guid>();

                foreach (var entityState in entityStates)
                {
                    var entityId = new Guid(entityState.EntityId);
                    usedEntities.Add(entityId);

                    if (m_entities.TryGetValue(entityId, out var entity))
                    {
                        entity.SetState(entityState, response.TimeStamp);
                    }
                    else
                    {
                        SpawnRemoteEntity(m_entityPrefab, entityId, entityState, response.TimeStamp);
                    }
                }

                var deletedEntities = m_entities.Keys.Except(usedEntities);

                foreach (var deletedEntityId in deletedEntities)
                {
                    if (!m_entities[deletedEntityId].HasLocalAuthority)
                    {
                        Destroy(m_entities[deletedEntityId].gameObject);
                    }
                }
            });
        }

        #region Helper
        /// <summary>
        /// Spawns new <see cref="NetworkEntity"/> having local authority.
        /// </summary>
        /// <param name="prefab"></param>
        /// <param name="onEntitySpawned"></param>
        public void SpawnLocalEntity(NetworkEntity prefab, EntitySpawnCallback onEntitySpawned)
        {
            Assert.IsTrue(Instance.IsConnected);
            Assert.IsTrue(Authentication.IsAuthenticated);
            prefab.HasLocalAuthority = true;

            Message message = new Message
            {
                UserId  = Authentication.ClientId,
                Request = "register_entity",
                Arg     = null,
            };

            SendToServer(message, response => OnRegisterFinished(response, prefab, onEntitySpawned));
        }

        private void OnRegisterFinished(Response response, NetworkEntity entity, EntitySpawnCallback onEntitySpawned)
        {
            switch (response.Result)
            {
                case ERPCResult.Ok:
                    break;
                case ERPCResult.Error:
                    Debug.Log("Failed to register entity.");
                    onEntitySpawned?.Invoke(null, false);

                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            EnqueueMainThreadCommand(() =>
            {
                Guid newId;

                try
                {
                    newId = Guid.Parse(response.Args);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Invalid Guid : {response.Args}");

                    throw;
                }

                var spawned = Instantiate(entity);
                spawned.NetworkId = newId;

                RegisterEntity(spawned);

                onEntitySpawned?.Invoke(spawned, true);
            });
        }

        private void SpawnRemoteEntity(NetworkEntity prefab, Guid networkId, NetworkEntityState initialState, int timeStamp)
        {
            var spawned = Instantiate(prefab);
            spawned.NetworkId         = networkId;
            spawned.HasLocalAuthority = false;

            spawned.SetState(initialState, timeStamp);

            RegisterEntity(spawned);
        }

        private void RegisterEntity(NetworkEntity entity)
        {
            Instance.m_entities.Add(entity.NetworkId, entity);
        }

        internal void UnregisterEntity(NetworkEntity entity)
        {
            Instance.m_entities.Remove(entity.NetworkId);
        }
        #endregion
    }
}