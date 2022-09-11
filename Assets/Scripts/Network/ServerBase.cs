using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ServerBase : MonoBehaviour
{
    #region Variables

    public NetworkManager networkManager;

    public ServerPrefs prefs;

    public int localPlayers;

    protected Dictionary<ulong, PlayerInfo> clients = new Dictionary<ulong, PlayerInfo>();

    #endregion

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
        if (clients.Count < prefs.maxPlayers)
        {
            var cinfo = JsonUtility.FromJson<ConnectionPayload>(payload);
            if (cinfo.password == prefs.password || String.IsNullOrEmpty(prefs.password))
            {
                response.Approved = true;
                response.CreatePlayerObject = false;
                clients.Add(request.ClientNetworkId, cinfo.client);
                SpesLogger.Detail("Клиенту " + request.ClientNetworkId + " одобрен вход");
                return;
            }
            else
            {
                SpesLogger.Detail("Клиент " + request.ClientNetworkId + " предоставил неверный пароль");
            }
        }
        else
        {
            SpesLogger.Detail("Клиенту " + request.ClientNetworkId + " запрещен вход в переполненную комнату");
        }
    }

    #endregion

    #region Functions

    public void HostGame(ushort port = 2545)
    {
        UnityTransport net = networkManager.GetComponent<UnityTransport>();
        net.ConnectionData.Port = port;

        ConnectionPayload payload = new ConnectionPayload() { client = GameBase.client.clientInfo, password = "" };
        string jsonData = JsonUtility.ToJson(payload);
        networkManager.NetworkConfig.ConnectionData = System.Text.Encoding.UTF8.GetBytes(jsonData);

        SetupManagerCallbacks();
        SetupSceneCallbacks();

        networkManager.StartHost();

        networkManager.SceneManager.LoadScene("GameScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    public void SetupSingleDevice()
    {
        networkManager.NetworkConfig.ConnectionApproval = false;
        ConnectionPayload payload = new ConnectionPayload() { client = GameBase.client.clientInfo, password = "" };
        string jsonData = JsonUtility.ToJson(payload);
        networkManager.NetworkConfig.ConnectionData = System.Text.Encoding.UTF8.GetBytes(jsonData);

        SetupSceneCallbacks();

        networkManager.StartHost();

        networkManager.SceneManager.LoadScene("GameScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    public void ClearAll()
    {
        clients.Clear();
        networkManager.Shutdown();
    }

    protected void SetupManagerCallbacks()
    {
        networkManager.OnClientConnectedCallback += OnConnected;
        networkManager.OnClientDisconnectCallback += OnDisconnected;
        networkManager.OnTransportFailure += OnTransportFailure;
        networkManager.OnServerStarted += OnServerStarted;
        networkManager.ConnectionApprovalCallback += ApproveClient;
    }

    protected void SetupSceneCallbacks()
    {
        if (networkManager.SceneManager != null)
        {
            networkManager.SceneManager.OnLoad += OnLoadStarted;
            networkManager.SceneManager.OnLoadComplete += OnInstanceLoadComplete;
            networkManager.SceneManager.OnLoadEventCompleted += OnAllClientsLoaded;
        }
        else
        {
            SpesLogger.Warning("Попытка привязки к SceneManager, который не существует");
        }
    }

    #endregion

    #region UnityCallbacks

    public void OnDestroy()
    {
        if (networkManager)
        {
            networkManager.OnClientConnectedCallback -= OnConnected;
            networkManager.OnClientDisconnectCallback -= OnDisconnected;
            networkManager.OnTransportFailure -= OnTransportFailure;
            networkManager.OnServerStarted -= OnServerStarted;
            networkManager.ConnectionApprovalCallback -= ApproveClient;

            if (networkManager.SceneManager != null)
            {
                networkManager.SceneManager.OnLoad -= OnLoadStarted;
                networkManager.SceneManager.OnLoadComplete -= OnInstanceLoadComplete;
                networkManager.SceneManager.OnLoadEventCompleted -= OnAllClientsLoaded;
            }
        }
    }

    #endregion

}
