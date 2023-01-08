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

    public Dictionary<ulong, string> Clients { get; protected set; } = new Dictionary<ulong, string>();

    #endregion

    #region Callbacks

    private void OnAllClientsLoaded(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        String str = "��� �������, ����� ";
        foreach (var c in clientsTimedOut)
        {
            str += c + " ";
        }

        str += "��������� �����";
        SpesLogger.Detail(str);
    }

    private void OnInstanceLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        SpesLogger.Deb("������ " + clientId + " �������� ����� " + sceneName);
    }

    private void OnLoadStarted(ulong clientId, string sceneName, LoadSceneMode loadSceneMode, AsyncOperation asyncOperation)
    {
        SpesLogger.Deb("������ " + clientId + " ����� ������� ����� " + sceneName);
    }

    private void OnServerStarted()
    {
        SpesLogger.Detail("������ �������");
    }

    private void OnTransportFailure()
    {
        SpesLogger.Error("TransportFailure");
        networkManager.Shutdown();
        SceneManager.LoadScene("StartupScene");
    }

    private void OnDisconnected(ulong clientID)
    {
        SpesLogger.Detail("������ " + clientID + " ���������� " + (networkManager.IsServer ? "{������}" : "{������}"));
    }

    private void OnConnected(ulong clientID)
    {
        SpesLogger.Detail("������ " + clientID + " ����������� " + (networkManager.IsServer ? "{������}" : "{������}"));
    }

    private void ApproveClient(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        var payloadBytes = request.Payload;
        string payload = System.Text.Encoding.UTF8.GetString(payloadBytes);
        if (Clients.Count < prefs.maxPlayers)
        {
            var cinfo = JsonUtility.FromJson<ConnectionPayload>(payload);
            if (cinfo.password == prefs.password || String.IsNullOrEmpty(prefs.password))
            {
                response.Approved = true;
                response.CreatePlayerObject = false;
                Clients.Add(request.ClientNetworkId, cinfo.playerName);
                SpesLogger.Detail("������� " + request.ClientNetworkId + " ������� ����");
                return;
            }
            else
            {
                SpesLogger.Detail("������ " + request.ClientNetworkId + " ����������� �������� ������");
            }
        }
        else
        {
            SpesLogger.Detail("������� " + request.ClientNetworkId + " �������� ���� � ������������� �������");
        }
    }

    #endregion

    #region Functions

    public void HostGame(ushort port = 2545)
    {
        ClearAll();
        StartCoroutine(HostGameCoroutine(port));
    }

    public void SetupSingleDevice()
    {
        ClearAll();
        StartCoroutine(SetupSingleDeviceCoroutine());
    }

    public void ClearAll()
    {
        Clients.Clear();
        networkManager.Shutdown();
        UnbindAll();
    }

    protected void UnbindAll()
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

    protected IEnumerator HostGameCoroutine(ushort port)
    {
        while (networkManager.ShutdownInProgress)
        {
            yield return null;
        }
        UnityTransport net = networkManager.GetComponent<UnityTransport>();
        net.ConnectionData.Port = port;

        ConnectionPayload payload = new ConnectionPayload() { playerName = GameBase.client.playerName, password = "" };
        string jsonData = JsonUtility.ToJson(payload);
        networkManager.NetworkConfig.ConnectionData = System.Text.Encoding.UTF8.GetBytes(jsonData);

        SetupManagerCallbacks();
        SetupSceneCallbacks();

        networkManager.StartHost();

        networkManager.SceneManager.LoadScene("GameScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    protected IEnumerator SetupSingleDeviceCoroutine()
    {
        while (networkManager.ShutdownInProgress)
        {
            yield return null;
        }
        networkManager.NetworkConfig.ConnectionApproval = false;
        ConnectionPayload payload = new ConnectionPayload() { playerName = GameBase.client.playerName, password = "" };
        string jsonData = JsonUtility.ToJson(payload);
        networkManager.NetworkConfig.ConnectionData = System.Text.Encoding.UTF8.GetBytes(jsonData);

        SetupSceneCallbacks();

        networkManager.StartHost();

        Clients.Add(networkManager.LocalClientId, GameBase.client.playerName);

        networkManager.SceneManager.LoadScene("GameScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
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
            SpesLogger.Warning("������� �������� � SceneManager, ������� �� ����������");
        }
    }

    #endregion

    #region UnityCallbacks

    public void OnDestroy()
    {
        UnbindAll();
    }

    #endregion

}
