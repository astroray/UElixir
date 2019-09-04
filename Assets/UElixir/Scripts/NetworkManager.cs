using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine.Assertions;

namespace UElixir
{
    [Serializable]
    public class ElixirCommand
    {
        [JsonProperty("user_id")]
        public int UserId { get; set; }
        [JsonProperty("command")]
        public string Command { get; set; }
        [JsonProperty("args")]
        public string Args { get; set; }

        [JsonIgnore]
        public bool RequireResponse { get; set; } = true;
        [JsonIgnore]
        public string Response { get; set; }

        public override string ToString()
        {
            return $"{JsonSerializer.SerializeToString(this)}\r\n";
        }

        public byte[] ToByteArray()
        {
            return Encoding.UTF8.GetBytes(ToString());
        }
    }

    public class NetworkManager : MonoBehaviour
    {
        private static NetworkManager m_instance;
        public static  NetworkManager Instance => m_instance;

        [SerializeField]
        private string m_hostName = "localhost";
        [SerializeField]
        private int m_port = 4000;
        [SerializeField]
        private bool m_isConnected;
        [SerializeField]
        private NetworkUnit m_unitPrefab;
        [SerializeField]
        private NetworkUnit m_clientUnit;

        private Dictionary<int, NetworkUnit>      m_networkUnits = new Dictionary<int, NetworkUnit>();
        private Dictionary<int, NetworkTransform> m_transforms;

        public async void RegisterUnit(NetworkUnit networkUnit)
        {
            if (networkUnit.Owned)
            {
                m_clientUnit = networkUnit;
                int id = await Authenticate();
                networkUnit.NetworkId = id;
            }

            m_networkUnits[networkUnit.NetworkId] = networkUnit;
        }

        private void Awake()
        {
            if (Instance == null)
            {
                m_instance = this;
            }
            else if (Instance != this)
            {
                Debug.LogError("NetworkManager should be one at a scene.");
            }
        }

        private void Start()
        {
            StartConnect();

            Assert.IsNotNull(m_clientUnit);
            RegisterUnit(m_clientUnit);
        }

        private       TcpClient               m_client;
        private       CancellationTokenSource m_cancellation = new CancellationTokenSource();
        private       Queue<ElixirCommand>    m_commandQueue = new Queue<ElixirCommand>();
        private const int                     m_waitInterval = 10;
        private       bool                    m_waitForRespond;

        private void StartConnect()
        {
            Task.Run(ConnectToServer, m_cancellation.Token);
        }

        private void OnDestroy()
        {
            m_client.Close();
            m_cancellation.Cancel();
        }

        // TODO: Impl automatic reconnect when connection is lost.
        private bool TryConnect()
        {
            if (m_client.Connected)
            {
                return true;
            }

            Debug.Log("Try to connect to server...");

            try
            {
                m_client.Connect(m_hostName, m_port);
            }
            catch (Exception e)
            {
                Debug.Log(e);

                return false;
            }

            return m_client.Connected;
        }

        private void ConnectToServer()
        {
            while (true)
            {
                m_client         = new TcpClient();
                m_client.NoDelay = true;

                while (TryConnect() == false)
                {
                    Thread.Sleep(5000);
                }

                try
                {
                    using (var stream = m_client.GetStream())
                    {
                        while (m_client.Connected)
                        {
                            // Wait for command
                            while (m_commandQueue.Count == 0)
                            {
                                Thread.Sleep(m_waitInterval);
                            }

                            // Send command to server
                            Assert.IsTrue(stream.CanWrite);
                            var command = m_commandQueue.Dequeue();
                            var data    = command.ToByteArray();
                            stream.Write(data, 0, data.Length);
                            Debug.Log($"Message was sent : {command}");

                            if (!command.RequireResponse)
                            {
                                m_waitForRespond = false;

                                continue;
                            }

                            // Wait for respond
                            while (!stream.DataAvailable)
                            {
                                Debug.Log("Wait for respond");
                                Thread.Sleep(m_waitInterval);

                                if (!m_client.Connected)
                                {
                                    break;
                                }
                            }

                            // Read respond
                            using (var memoryStream = new MemoryStream())
                            {
                                var buffer = new byte[1024];

                                do
                                {
                                    int length = stream.Read(buffer, 0, buffer.Length);
                                    memoryStream.Write(buffer, 0, length);
                                } while (stream.DataAvailable);

                                command.Response = Encoding.UTF8.GetString(memoryStream.ToArray());
                                Debug.Log($"Got response : {command.Response}");
                            }

                            m_waitForRespond = false;
                        }
                    }
                }
                catch (SocketException e)
                {
                    Debug.Log(e);
                    m_client.Close();
                }
            }
        }

        private async void Echo()
        {
            if (m_waitForRespond)
            {
                return;
            }

            ElixirCommand command = new ElixirCommand
            {
                UserId  = 0,
                Command = "echo",
                Args    = "my echo message"
            };

            SendCommand(command);

            while (m_waitForRespond)
            {
                await Task.Delay(m_waitInterval);
            }

            Debug.Log($"Data received : {command.Response}");
        }

        private void SendCommand(ElixirCommand command)
        {
            m_waitForRespond = true;

            m_commandQueue.Enqueue(command);
        }

        private async Task<int> Authenticate()
        {
            ElixirCommand command = new ElixirCommand
            {
                UserId  = -1,
                Command = "authenticate",
                Args    = null,
            };

            SendCommand(command);

            while (m_waitForRespond)
            {
                await Task.Delay(m_waitInterval);
            }

            return int.Parse(command.Response);
        }

        private async void Update()
        {
            //Debug.Log("Start update");

            if (m_client == null || !m_client.Connected || m_clientUnit.NetworkId == -1)
            {
                return;
            }

            ElixirCommand reportUnitState = new ElixirCommand
            {
                UserId          = m_clientUnit.NetworkId,
                Command         = "report_unit_state",
                Args            = m_clientUnit.NetworkTransform.ToString(),
                RequireResponse = false,
            };

            SendCommand(reportUnitState);

            ElixirCommand nextUnitTransforms = new ElixirCommand
            {
                UserId          = m_clientUnit.NetworkId,
                Command         = "get_unit_states",
                Args            = null,
                RequireResponse = true,
            };

            SendCommand(nextUnitTransforms);

            while (m_waitForRespond)
            {
                await Task.Delay(m_waitInterval);
            }

            //Debug.Log(nextUnitTransforms.Response);
            m_transforms = JsonSerializer.DeserializeFromString<Dictionary<int, NetworkTransform>>(nextUnitTransforms.Response);

            //foreach (var pair in m_transforms)
            //{
            //    Debug.Log($"{pair.Key} : {pair.Value}");
            //}

            //Debug.Log("End update");
        }

        private void FixedUpdate()
        {
            if (m_transforms == null)
            {
                return;
            }

            foreach (var pair in m_transforms)
            {
                int id = pair.Key;

                if (!m_networkUnits.ContainsKey(id))
                {
                    var unit = Instantiate(m_unitPrefab);
                    unit.NetworkId = id;
                    RegisterUnit(unit);
                }

                if (m_networkUnits[id].Owned)
                {
                    continue;
                }

                m_networkUnits[id].NetworkTransform = pair.Value;
            }
        }
    }
}