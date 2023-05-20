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

		if (players.Count >= GameBase.Server.GetMaxPlayersCount())
		{
			//Reconnection
			if (CheckReconnection(payload))
			{
				int reconnectedPlayer = FindPlayerByToken(payload.playerToken);

				NetworkManager.Singleton.DisconnectClient(players[reconnectedPlayer].clientID, "Logged in from other device");

				players[reconnectedPlayer] = CreatePlayer(payload, request.ClientNetworkId, false);

				response.Approved = true;
				return;
			}

			response.Reason = "Server is full";
			response.Approved = false;
			return;
		}

		//Simple client
		PlayerDescriptor player = CreatePlayer(payload, request.ClientNetworkId, false);
		players.Add(player);

		response.Approved = true;
	}

	private void NetworkManager_OnClientConnectedCallback(ulong obj)
	{
		if (networkManager.ConnectedClientsIds.Count == serverPrefs.maxRemotePlayers)
		{
			onAllPlayersConnected?.Invoke(this, EventArgs.Empty);
		}
	}


	private void NetworkManager_OnClientDisconnectCallback(ulong clientID)
	{
		if (SceneManager.GetActiveScene().name == Scenes.LobbyScene.ToString())
		{
			var playerToRemove = GetRemotePlayerByClientID(clientID);
			players.Remove(playerToRemove);
		}
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

	public void SetMaxRemotePlayersCount(int count)
	{
		serverPrefs.maxRemotePlayers = count;
	}

	public void SetLocalPlayersCount(int count)
	{
		gamePrefs.localPlayers = count;
	}

	public int GetMaxPlayersCount()
	{
		return gamePrefs.localPlayers + serverPrefs.maxRemotePlayers;
	}

	public int GetMaxRemotePlayersCount()
	{
		return serverPrefs.maxRemotePlayers;
	}

	public void HostGame(ushort? port = null)
	{
		bNetMode = true;

		ClearAll();
		EnsureShutdown();

		SetupBindings();

		UpdateConnectionPayload();
		CreateLocalPlayers();

		if (port != null)
		{
			var tr = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;
			tr.ConnectionData.Port = port.Value;
		}

		NetworkManager.Singleton.StartHost();
		SceneLoader.LoadNetwork(Scenes.LobbyScene);
	}

	private void SetupBindings()
	{
		networkManager.OnTransportFailure += OnTransportFailure;
		networkManager.ConnectionApprovalCallback += ApproveClient;
		networkManager.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
		networkManager.OnClientDisconnectCallback += NetworkManager_OnClientDisconnectCallback;
	}

	public void SetupSingleDevice()
	{
		ClearAll();
		EnsureShutdown();
		UpdateConnectionPayload();
		CreateLocalPlayers();

		SetupBindings();

		serverPrefs.maxRemotePlayers = 0;
		bNetMode = false;

		NetworkManager.Singleton.StartHost();
		SceneLoader.LoadNetwork(Scenes.GameScene);
	}

	public void KickPlayer(ulong clientID)
	{
		if (networkManager.LocalClientId == clientID)
			return;

		var player = GetRemotePlayerByClientID(clientID);

		networkManager.DisconnectClient(player.clientID, "Kicked by server");
	}

	public void ClearAll()
	{
		UnbindAll();
		players.Clear();
		networkManager.Shutdown();
	}

	private void CreateLocalPlayers()
	{
		for (int i = 0; i < gamePrefs.localPlayers; i++)
		{
			ConnectionPayload payload = JsonUtility.FromJson<ConnectionPayload>(Encoding.ASCII.GetString(
				NetworkManager.Singleton.NetworkConfig.ConnectionData));

			players.Add(CreatePlayer(payload, NetworkManager.ServerClientId, true));
		}
	}

	private void UpdateConnectionPayload()
	{
		ConnectionPayload payload = new ConnectionPayload();
		payload.pawnSkinID = GameBase.Storage.CurrentPawnSkinID;
		payload.boardSkin = GameBase.Storage.CurrentBoardSkinID;
		payload.playerName = GameBase.Client.playerName;
		payload.playerToken = UnityEngine.Random.Range(0, 10000).ToString();

		NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.ASCII.GetBytes(JsonUtility.ToJson(payload));
	}

	private PlayerDescriptor CreatePlayer(ConnectionPayload payload, ulong clientID, bool bLocal)
	{
		PlayerDescriptor player = new PlayerDescriptor();
		player.bLocal = bLocal;
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
		float shutdownCheckDelay = 0.1f;

		while (NetworkManager.Singleton.ShutdownInProgress)
		{
			new WaitForSeconds(shutdownCheckDelay);
		}
	}

	private void UnbindAll()
	{
		if (networkManager)
		{
			networkManager.OnTransportFailure -= OnTransportFailure;
			networkManager.OnClientConnectedCallback -= NetworkManager_OnClientConnectedCallback;
			networkManager.ConnectionApprovalCallback -= ApproveClient;
		}
	}

	#endregion

	#region UnityCallbacks

	private void Start()
	{
		networkManager = NetworkManager.Singleton;
	}

	private void OnDestroy()
	{
		UnbindAll();
	}

	#endregion

}
