using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ServerBase : MonoBehaviour
{
	#region HelperClasses

	public class PlayerDescriptorEventArgs : EventArgs
	{
		public PlayerDescriptor playerDescriptor;
	}

	#endregion

	#region Variables

	public event EventHandler<PlayerDescriptorEventArgs> onPlayerConnected;

	public event EventHandler<PlayerDescriptorEventArgs> onPlayerDisconnected;

	public event EventHandler onAllPlayersConnected;

	public const int defaultPort = 2545;

	[SerializeField] private ServerPrefs serverPrefs = new ServerPrefs() { maxConnectPayloadSize = 200, reconnectionTime = 60 };

	private GamePrefs gamePrefs;

	private NetworkManager networkManager;

	public List<PlayerDescriptor> players = new List<PlayerDescriptor>();

	public bool bNetMode;

	#endregion

	#region Callbacks

	private void OnTransportFailure()
	{
		networkManager.Shutdown();
		SceneManager.LoadScene("StartupScene");
	}

	private void ApproveClient(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
	{
		//Host
		if (request.ClientNetworkId == NetworkManager.ServerClientId)
		{
			response.Approved = true;
			return;
		}

		//Payload DOS protection
		if (request.Payload.Length > serverPrefs.maxConnectPayloadSize)
		{
			response.Approved = false;
			return;
		}

		ConnectionPayload payload = GetPayload(request);

		if (players.Count >= serverPrefs.maxPlayers)
		{
			//Reconnection
			if (CheckReconnection(payload))
			{
				int reconnectedPlayer = FindPlayerByToken(payload.playerToken);

				NetworkManager.Singleton.DisconnectClient(players[reconnectedPlayer].clientID, "Logged in from other device");

				players[reconnectedPlayer] = CreatePlayer(payload, request.ClientNetworkId);

				response.Approved = true;
				return;
			}

			response.Reason = "Server is full";
			response.Approved = false;
			return;
		}

		//Simple client or host
		PlayerDescriptor player = CreatePlayer(payload, request.ClientNetworkId);
		players.Add(player);

		response.Approved = true;
	}

	#endregion

	#region Functions

	public PlayerDescriptor GetPlayerByOrder(int order)
	{
		return players.Find(x =>
		{
			return x.playerOrder == order;
		});
	}

	public PlayerDescriptor GetRemotePlayerByClientID(ulong clientID)
	{
		return players.Find(x =>
		{
			return x.clientID == clientID;
		});
	}

	public PlayerDescriptor GetPlayerByToken(string playerToken)
	{
		return players.Find(x =>
		{
			return x.playerToken == playerToken;
		});
	}

	public GamePrefs GetGamePrefs()
	{
		return gamePrefs;
	}

	public void SetGamePrefs(GamePrefs prefs)
	{
		gamePrefs = prefs;
	}

	public int GetPlayersCount()
	{
		return players.Count;
	}

	public void SetMaxPlayersCount(int count)
	{
		serverPrefs.maxPlayers = count;
	}
	public void SetLocalPlayersCount(int count)
	{
		gamePrefs.localPlayers = count;
	}

	public int GetMaxPlayersCount()
	{
		return serverPrefs.maxPlayers;
	}

	public void HostGame(ushort? port = null)
	{
		bNetMode = true;

		ClearAll();
		EnsureShutdown();
		UpdateConnectionPayload();
		CreateLocalPlayers();

		if (port != null)
		{
			var tr = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;
			tr.ConnectionData.Port = port.Value;
		}

		networkManager.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;

		NetworkManager.Singleton.StartHost();
		SceneLoader.LoadNetwork(Scenes.GameScene);
	}

	private void NetworkManager_OnClientConnectedCallback(ulong obj)
	{
		if (networkManager.ConnectedClientsIds.Count == serverPrefs.maxPlayers)
		{
			onAllPlayersConnected?.Invoke(this, EventArgs.Empty);
		}
	}

	public void SetupSingleDevice()
	{
		ClearAll();
		EnsureShutdown();
		UpdateConnectionPayload();
		CreateLocalPlayers();

		serverPrefs.maxPlayers = 1;
		bNetMode = false;

		NetworkManager.Singleton.StartHost();
		SceneLoader.LoadNetwork(Scenes.GameScene);
	}

	private void CreateLocalPlayers()
	{
		for (int i = 0; i < gamePrefs.localPlayers; i++)
		{
			ConnectionPayload payload = JsonUtility.FromJson<ConnectionPayload>(Encoding.ASCII.GetString(
				NetworkManager.Singleton.NetworkConfig.ConnectionData));

			players.Add(CreatePlayer(payload, NetworkManager.ServerClientId));
		}
	}

	private void UpdateConnectionPayload()
	{
		ConnectionPayload payload = new ConnectionPayload();
		payload.pawnSkinID = GameBase.Storage.CurrentPawnSkin;
		payload.boardSkin = GameBase.Storage.CurrentBoardSkin;
		payload.playerName = GameBase.Client.playerName;
		payload.playerToken = UnityEngine.Random.Range(0, 10000).ToString();

		NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.ASCII.GetBytes(JsonUtility.ToJson(payload));
	}

	private PlayerDescriptor CreatePlayer(ConnectionPayload payload, ulong clientID)
	{
		PlayerDescriptor player = new PlayerDescriptor();
		player.clientID = clientID;
		player.playerOrder = players.Count;

		player.pawnSkinID = payload.pawnSkinID;
		player.boardSkinID = payload.boardSkin;
		player.playerName = payload.playerName;
		player.playerToken = payload.playerToken;

		return player;
	}

	private bool CheckReconnection(ConnectionPayload payload)
	{
		foreach (var player in players)
		{
			if (player.playerToken == payload.playerToken)
			{
				return true;
			}
		}
		return false;
	}

	private ConnectionPayload GetPayload(NetworkManager.ConnectionApprovalRequest request)
	{
		string payloadString = Encoding.ASCII.GetString(request.Payload);
		return JsonUtility.FromJson<ConnectionPayload>(payloadString);
	}

	private int FindPlayerByToken(string token)
	{
		for (int i = 0; i < players.Count; i++)
		{
			if (players[i].playerToken == token)
				return i;
		}
		return -1;
	}

	private void EnsureShutdown()
	{
		while (NetworkManager.Singleton.ShutdownInProgress)
		{
			float shutdownCheckDelay = 0.1f;
			new WaitForSeconds(shutdownCheckDelay);
		}
	}

	private void ClearAll()
	{
		players.Clear();
		networkManager.Shutdown();
	}

	private void UnbindAll()
	{
		if(networkManager)
		{
			networkManager.OnTransportFailure -= OnTransportFailure;
			networkManager.ConnectionApprovalCallback -= ApproveClient;
		}
	}

	#endregion

	#region UnityCallbacks

	private void Start()
	{
		networkManager = NetworkManager.Singleton;
		networkManager.ConnectionApprovalCallback += ApproveClient;
		networkManager.OnTransportFailure += OnTransportFailure;
	}

	private void OnDestroy()
	{
		UnbindAll();
	}

	#endregion

}
