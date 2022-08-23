using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System;
using UnityEngine.SceneManagement;
using Unity.Collections;

public struct NetworkString : INetworkSerializable
{
    private FixedString32Bytes info;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        string str = this.ToString();
        serializer.SerializeValue(ref str);

        if (serializer.IsReader)
        {
            info = new FixedString32Bytes(str);
        }
    }

    public override string ToString()
    {
        return info.ToString();
    }

    public static implicit operator string(NetworkString s) => s.ToString();
    public static implicit operator NetworkString(string s) => new NetworkString { info = new FixedString32Bytes(s) };
}

[Serializable]
public struct ClientInfo
{
    public NetworkString name;
    public int skinID;
}

[Serializable]
public struct ConnectPayload
{
    public ClientInfo client;
    public string password;
}

public class GameBase : MonoBehaviour
{
    #region Variables

    [SerializeField] private MenusLibrary menusLibrary;

    protected NetworkManager networkManager;

    protected ClientInfo clientInfo;

    #region ServerVariables

    public int maxClientsCount = 2;

    public string password = "";

    protected Dictionary<ulong, ClientInfo> clients = new Dictionary<ulong, ClientInfo>();


    #endregion

    #endregion

    #region StaticVariables

    public static GameBase instance;

    #endregion

    #region UnityCallbacks

    public void Awake()
    {
        if (instance)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(this);
        MenuBase.SetLibrary(menusLibrary);
        networkManager = GetComponent<NetworkManager>();
        ConfigureNetwork();
    }

    #endregion

    #region Functions

    #region Networking

    #region Callbacks

    private void OnAllClientsLoaded(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        String str = "Все клиенты, кроме ";
        foreach (var c in clientsTimedOut)
        {
            str += c + " ";
        }

        str += "загрузили карту";
        SpesLogger.Detail(str);
    }

    private void OnInstanceLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        SpesLogger.Deb("Клиент " + clientId + " загрузил карту " + sceneName);
    }

    private void OnLoadStarted(ulong clientId, string sceneName, LoadSceneMode loadSceneMode, AsyncOperation asyncOperation)
    {
        SpesLogger.Deb("Клиент " + clientId + " начал загруку карты " + sceneName);
    }

    private void OnServerStarted()
    {
        SpesLogger.Detail("Сервер запущен");
    }

    private void OnTransportFailure()
    {
        SpesLogger.Error("TransportFailure");
        networkManager.Shutdown();
        SceneManager.LoadScene("StartupScene");
    }

    private void OnDisconnected(ulong clientID)
    {
        SpesLogger.Detail("Клиент " + clientID + " отключился " + (networkManager.IsServer ? "{Сервер}" : "{Клиент}"));
    }

    private void OnConnected(ulong clientID)
    {
        SpesLogger.Detail("Клиент " + clientID + " подключился " + (networkManager.IsServer ? "{Сервер}" : "{Клиент}"));
    }

    private void ApproveClient(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        var payloadBytes = request.Payload;
        string payload = System.Text.Encoding.UTF8.GetString(payloadBytes);
        if (clients.Count < maxClientsCount)
        {
            var cinfo = JsonUtility.FromJson<ConnectPayload>(payload);
            if (cinfo.password == password || String.IsNullOrEmpty(password))
            {
                response.Approved = true;
                clients.Add(request.ClientNetworkId, cinfo.client);
            }
        }
    }


    #endregion

    protected void ConfigureNetwork()
    {
        networkManager.OnClientConnectedCallback += OnConnected;
        networkManager.OnClientDisconnectCallback += OnDisconnected;
        networkManager.OnTransportFailure += OnTransportFailure;
        networkManager.OnServerStarted += OnServerStarted;
        networkManager.ConnectionApprovalCallback += ApproveClient;
        if (networkManager.SceneManager != null)
        {
            networkManager.SceneManager.OnLoad += OnLoadStarted;
            networkManager.SceneManager.OnLoadComplete += OnInstanceLoadComplete;
            networkManager.SceneManager.OnLoadEventCompleted += OnAllClientsLoaded;
        }
    }

    public void HostGame(ushort port = 2545)
    {
        UnityTransport net = networkManager.GetComponent<UnityTransport>();
        net.ConnectionData.Port = port;

        ConnectPayload payload = new ConnectPayload() { client = clientInfo, password = "" };
        string jsonData = JsonUtility.ToJson(payload);
        networkManager.NetworkConfig.ConnectionData = System.Text.Encoding.UTF8.GetBytes(jsonData);

        networkManager.StartHost();
        networkManager.SceneManager.LoadScene("GameScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    public void ConnectToGame(string address, ushort port)
    {
        UnityTransport net = networkManager.GetComponent<UnityTransport>();
        net.ConnectionData.Address = address;
        net.ConnectionData.Port = port;

        ConnectPayload payload = new ConnectPayload() { client = clientInfo, password = "" };
        string jsonData = JsonUtility.ToJson(payload);
        networkManager.NetworkConfig.ConnectionData = System.Text.Encoding.UTF8.GetBytes(jsonData);

        networkManager.StartClient();
    }


    #endregion

    #endregion
}
