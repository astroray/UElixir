using System;
using System.Collections.Concurrent;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Assertions;

namespace UElixir
{
    public delegate void ResponseCallback(Response response);

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

        private TcpClient m_client = new TcpClient { NoDelay = true };

        public string HostName    => m_hostName;
        public int    Port        => m_port;
        public bool   IsConnected => m_client.Connected;

        public async Task Connect(Action<bool> onConnect)
        {
            try
            {
                var address = Dns.GetHostEntry(HostName).AddressList.First(ip => ip.AddressFamily == AddressFamily.InterNetwork);
                await m_client.ConnectAsync(address, Port);

                if (IsConnected)
                {
                    StartListenToServer();
                }
            }
            catch (Exception e)
            {
                onConnect?.Invoke(IsConnected);
                Debug.Log(e);
            }

            onConnect?.Invoke(IsConnected);
        }

        private Thread m_listener;

        private void StartListenToServer()
        {
            m_listener?.Abort();

            m_listener = new Thread(ListenToServer)
            {
                IsBackground = true,
            };

            m_listener.Start();
        }

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

                        var responseString = Encoding.UTF8.GetString(memoryStream.ToArray());

                        foreach (var line in responseString.Split('\n'))
                        {
                            Debug.Log($"Got response : {line}");
                            var response = JsonSerializer.DeserializeFromString<Response>(responseString);

                            while (m_responseCallbacks.Count > 0)
                            {
                                if (m_responseCallbacks.TryDequeue(out var callback))
                                {
                                    callback.Invoke(response);
                                    break;
                                }

                                Thread.Sleep(m_waitInterval);
                            }
                        }
                    }
                }
            }
        }

        private ConcurrentQueue<ResponseCallback> m_responseCallbacks = new ConcurrentQueue<ResponseCallback>();

        public void SendToServer(Message message, ResponseCallback responseCallback)
        {
            Assert.IsTrue(IsConnected);

            var stream = m_client.GetStream();
            Assert.IsTrue(stream.CanWrite);

            var packet = message.ToByteArray();
            stream.Write(packet, 0, packet.Length);

            Debug.Log($"Message was sent : {message}");

            m_responseCallbacks.Enqueue(responseCallback);
        }

        // ================== Regacy

        public async void RegisterUnit(NetworkUnit networkUnit)
        {
            if (networkUnit.Owned)
            {
                m_clientUnit = networkUnit;
            }

            m_networkUnits[networkUnit.NetworkId] = networkUnit;
        }

        private void Start()
        {
        }

        private       CancellationTokenSource m_cancellation = new CancellationTokenSource();
        private       Queue<Message>          m_commandQueue = new Queue<Message>();
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

            Message command = new Message
            {
                UserId  = 0,
                Arg     = "my echo message"
            };

            SendCommand(command);

            while (m_waitForRespond)
            {
                await Task.Delay(m_waitInterval);
            }

            Debug.Log($"Data received : {command.Response}");
        }

        private void SendCommand(Message command)
        {
            m_waitForRespond = true;

            m_commandQueue.Enqueue(command);
        }

        private async void Update_nonono()
        {
            //Debug.Log("Start update");

            if (m_client == null || !m_client.Connected || m_clientUnit.NetworkId == -1)
            {
                return;
            }

            Message reportUnitState = new Message
            {
                UserId          = m_clientUnit.NetworkId,
                Arg             = m_clientUnit.NetworkTransform.ToString(),
                RequireResponse = false,
            };

            SendCommand(reportUnitState);

            Message nextUnitTransforms = new Message
            {
                UserId          = m_clientUnit.NetworkId,
                Arg             = null,
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