using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UnityLobbyService : MonoBehaviour
{
	private const string KEY_RELAY_JOIN_CODE = "RelayJoinCode";
	public static UnityLobbyService Instance { get; private set; }

	private Lobby joinedLobby;
	private float heartbeatTimerMax = 15f;
	private float currentHeartbeatTImer;

	private float listLobbiesTimerMax = 5f;
	private float listLobbiesTimer;

	public event EventHandler OnCreateLobbyStarted;
	public event EventHandler OnCreateLobbyFailed;

	public event EventHandler OnJoinStarted;
	public event EventHandler OnJoinFailed;

	public event EventHandler<OnLobbyListChangedEventArgs> OnLobbyListChanged;

	public class OnLobbyListChangedEventArgs : EventArgs
	{
		public List<Lobby> lobbyList;
	}

	private void Awake()
	{
		Instance = this;

		InitializeUnityAuthentifcationAsync();
		currentHeartbeatTImer = heartbeatTimerMax;
		listLobbiesTimer = listLobbiesTimerMax;
	}

	private void Update()
	{
		HandleHeartbeat();
	}

	public async void CreateLobbyAsync(string lobbyName, bool isPrivate)
	{
		OnCreateLobbyStarted?.Invoke(this, EventArgs.Empty);
		try
		{
			GameBase.Instance.LoadingScreen.Setup();
			joinedLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, GameBase.Server.GetMaxRemotePlayersCount() + 1, new CreateLobbyOptions() { IsPrivate = isPrivate });

			Allocation allocation = await AllocateRelayAsync();

			string joinCode = await GetRelayJoinCodeAsync(allocation);

			await LobbyService.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions()
			{
				Data = new Dictionary<string, DataObject>
				{
					{KEY_RELAY_JOIN_CODE, new DataObject(DataObject.VisibilityOptions.Member, joinCode)}
				}
			});

			NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
				allocation.RelayServer.IpV4,
				(ushort)allocation.RelayServer.Port,
				allocation.AllocationIdBytes,
				allocation.Key,
				allocation.ConnectionData);

			GameBase.Instance.LoadingScreen.Hide();
			GameBase.Server.HostGame();
			SceneLoader.LoadNetwork(Scenes.LobbyScene);
		}
		catch (LobbyServiceException exception)
		{
			GameBase.Instance.LoadingScreen.Hide();
			Debug.Log(exception);
			OnCreateLobbyFailed?.Invoke(this, EventArgs.Empty);
		}
	}

	public async void QuickJoinAsync()
	{
		OnJoinStarted?.Invoke(this, EventArgs.Empty);
		try
		{
			GameBase.Instance.LoadingScreen.Setup();
			joinedLobby = await LobbyService.Instance.QuickJoinLobbyAsync();

			string relayJoinCode = joinedLobby.Data[KEY_RELAY_JOIN_CODE].Value;

			JoinAllocation joinAllocation = await JoinRelay(relayJoinCode);

			NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
				joinAllocation.RelayServer.IpV4,
				(ushort)joinAllocation.RelayServer.Port,
				joinAllocation.AllocationIdBytes,
				joinAllocation.Key,
				joinAllocation.ConnectionData,
				joinAllocation.HostConnectionData,
				false);

			GameBase.Instance.LoadingScreen.Hide();
			GameBase.Client.ConnectToHost("", 0);
		}
		catch (LobbyServiceException exception)
		{
			GameBase.Instance.LoadingScreen.Hide();
			OnJoinFailed?.Invoke(this, EventArgs.Empty);
			Debug.Log(exception);
		}
	}

	public Lobby GetLobby()
	{
		return joinedLobby;
	}

	public async void JoinLobbyByCodeAsync(string code)
	{
		OnJoinStarted?.Invoke(this, EventArgs.Empty);
		try
		{
			GameBase.Instance.LoadingScreen.Setup();
			joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(code);

			string relayJoinCode = joinedLobby.Data[KEY_RELAY_JOIN_CODE].Value;

			JoinAllocation joinAllocation = await JoinRelay(relayJoinCode);
			NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
				joinAllocation.RelayServer.IpV4,
				(ushort)joinAllocation.RelayServer.Port,
				joinAllocation.AllocationIdBytes,
				joinAllocation.Key,
				joinAllocation.ConnectionData,
				joinAllocation.HostConnectionData,
				false);

			GameBase.Instance.LoadingScreen.Hide();
			GameBase.Client.ConnectToHost("", 0);
		}
		catch (LobbyServiceException exception)
		{
			GameBase.Instance.LoadingScreen.Hide();
			OnJoinFailed?.Invoke(this, EventArgs.Empty);
			Debug.Log(exception);
		}
	}

	public async void DeleteLobbyAsync()
	{
		try
		{
			if (joinedLobby != null)
			{
				await LobbyService.Instance.DeleteLobbyAsync(joinedLobby.Id);

				joinedLobby = null;
			}
		}
		catch (LobbyServiceException exception)
		{
			Debug.Log(exception);
		}
	}

	public async void LeaveLobbyAsync()
	{
		if (joinedLobby != null)
		{
			try
			{
				await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
				joinedLobby = null;
			}
			catch (LobbyServiceException exception)
			{
				Debug.Log(exception);
			}
		}
	}

	public async void KickPlayerAsync(string playerId)
	{
		if (IsLobbyHost())
		{
			try
			{
				await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, playerId);
			}
			catch (LobbyServiceException exception)
			{
				Debug.Log(exception);
			}
		}
	}

	private void HandleHeartbeat()
	{
		if (IsLobbyHost())
		{
			currentHeartbeatTImer -= Time.deltaTime;
			if (currentHeartbeatTImer <= 0f)
			{
				currentHeartbeatTImer = heartbeatTimerMax - currentHeartbeatTImer;

				LobbyService.Instance.SendHeartbeatPingAsync(joinedLobby.Id);
			}
		}
	}

	private bool IsLobbyHost()
	{
		return joinedLobby != null && joinedLobby.HostId == AuthenticationService.Instance.PlayerId;
	}

	private async void InitializeUnityAuthentifcationAsync()
	{
		if (UnityServices.State != ServicesInitializationState.Initialized)
		{
			InitializationOptions options = new InitializationOptions();
			options.SetProfile(UnityEngine.Random.Range(0, 100000).ToString());
			await UnityServices.InitializeAsync(options);

			await AuthenticationService.Instance.SignInAnonymouslyAsync();
		}
	}

	private async Task<Allocation> AllocateRelayAsync()
	{
		try
		{
			Allocation allocation = await RelayService.Instance.CreateAllocationAsync(GameBase.Server.GetMaxRemotePlayersCount());

			return allocation;
		}
		catch (RelayServiceException exception)
		{
			Debug.Log(exception);

			return default;
		}
	}

	private async Task<string> GetRelayJoinCodeAsync(Allocation allocation)
	{
		try
		{
			string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

			return relayJoinCode;
		}
		catch (RelayServiceException exception)
		{
			Debug.Log(exception);

			return "";
		}
	}

	private async Task<JoinAllocation> JoinRelay(string joinCode)
	{
		try
		{
			JoinAllocation joinAlloc = await RelayService.Instance.JoinAllocationAsync(joinCode);

			return joinAlloc;
		}
		catch (RelayServiceException exception)
		{
			Debug.Log(exception);

			return default;
		}
	}
}
