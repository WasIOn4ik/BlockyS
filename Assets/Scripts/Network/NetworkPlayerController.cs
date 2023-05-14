using UnityEngine;
using Unity.Netcode;
using System;

public class NetworkPlayerController : NetworkBehaviour, IPlayerController
{
	#region Variables

	public event EventHandler onNetworkCosmeticChanged;

	[SerializeField] private InGameHUD hudPrefab;

	private NetworkVariable<PlayerCosmetic> cosmetic = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

	private NetworkVariable<PlayerNetworkedInfo> playerInfo = new();

	private InputComponent inputComp;

	private InGameHUD hud;

	private Camera cam;


	#endregion

	#region UnityCallbacks

	private void Awake()
	{
		inputComp = GetComponent<InputComponent>();
	}

	#endregion

	#region Overrides

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();

		if (IsServer)
		{
			var info = GetPlayerInfo();
			info.state = EPlayerState.Operator;
			SetPlayerInfo(info);
		}
		else
		{
			hud = Instantiate(hudPrefab);
			hud.SetInputComponent(inputComp);

			playerInfo.OnValueChanged += OnPlayerInfoChanged;
		}

		if (IsOwner)
		{
			cam = Camera.main;
			AllignCamera();
			GameplayBase.instance.cameraAnimator.AnimateCamera();

			SpesLogger.Detail("Skins Selected: " + GameBase.storage.CurrentBoardSkin + " " + GameBase.storage.CurrentPawnSkin);
			cosmetic.Value = new PlayerCosmetic() { boardSkinID = GameBase.storage.CurrentBoardSkin, pawnSkinID = GameBase.storage.CurrentPawnSkin };
			onNetworkCosmeticChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	public override void OnDestroy()
	{
		if (cam && cam.transform.parent == transform)
		{
			cam.transform.SetParent(null);
		}

		base.OnDestroy();
	}

	#endregion

	#region Callbacks

	private void OnPlayerInfoChanged(PlayerNetworkedInfo previousValue, PlayerNetworkedInfo newValue)
	{
		if (!IsServer)
			hud.SetWallsCount(newValue.WallCount);
	}

	#endregion

	#region IPlayerController

	/// <summary>
	/// In NetworkController'e It calls from client and handles in ServerRPC
	/// </summary>
	/// <param name="turn"></param>
	public void EndTurn(Turn turn)
	{
		if (GetPlayerInfo().state != EPlayerState.ActivePlayer)
			return;

		if (IsOwner)
		{
			inputComp.turnValid -= hud.OnTurnValidationChanged;
		}
		SpesLogger.Deb("Local network player " + GetPlayerInfo().playerOrder + " ends turn");

		EndTurnServerRpc(turn);
	}

	public MonoBehaviour GetMono()
	{
		return this;
	}

	public PlayerInGameInfo GetPlayerInfo()
	{
		return playerInfo.Value;
	}

	/// <summary>
	/// In NetworkController calls by server and handles in ClientRpc
	/// </summary>
	public void StartTurn()
	{
		SpesLogger.Deb("Start of remote player turn: " + GetPlayerInfo().playerOrder);

		var info = GetPlayerInfo();
		info.state = EPlayerState.ActivePlayer;
		SetPlayerInfo(info);

		StartTurnClientRpc();
	}

	public void SetPlayerInfo(PlayerInGameInfo inf)
	{
		playerInfo.Value = inf;
	}

	public PlayerCosmetic GetCosmetic()
	{
		SpesLogger.Detail("Player: " + name + " B-Skin: " + GameBase.storage.CurrentBoardSkin + " _ P-Skin: " + GameBase.storage.CurrentPawnSkin);
		return cosmetic.Value;
	}

	public void UpdateTurn(int active)
	{
		UpdateTurnClientRpc(active);
	}

	#endregion

	#region Functions

	/// <summary>
	/// Aligns controller to right position
	/// </summary>
	/// <returns>������� ��������� ������ �� ����������</returns>
	private Vector3 AllignCamera()
	{
		cam.transform.SetParent(transform);
		cam.transform.localPosition = Vector3.zero;
		cam.transform.localRotation = Quaternion.identity;
		var cameraPosition = cam.transform.position;
		var cameraRotation = cam.transform.rotation;
		//Calculates new Camera position
		Vector3 pos = GetPlayerInfo().pawn.transform.position + GetPlayerInfo().pawn.transform.forward * GameBase.instance.gameRules.cameraBackwardOffset + Vector3.up * GameBase.instance.gameRules.cameraHeight;
		transform.SetPositionAndRotation(pos, cameraRotation);
		return cameraPosition;
	}

	#endregion

	#region RPCs

	[ServerRpc(RequireOwnership = true, Delivery = RpcDelivery.Reliable)]
	private void EndTurnServerRpc(Turn turn)
	{
		SpesLogger.Deb("Remote player ended turn " + GetPlayerInfo().playerOrder);

		var info = GetPlayerInfo();
		info.state = EPlayerState.Operator;
		SetPlayerInfo(info);

		GameplayBase.instance.S_EndTurn(this, turn);
	}

	[ClientRpc(Delivery = RpcDelivery.Reliable)]
	private void StartTurnClientRpc()
	{
		SpesLogger.Deb($"Local player {GetPlayerInfo().playerOrder} started turn");

		if (IsOwner)
		{
			inputComp.turnValid += hud.OnTurnValidationChanged;

			cam.transform.position = AllignCamera();
			GameplayBase.instance.cameraAnimator.AnimateCamera();
		}

		inputComp.UpdateTurnValid(false);
	}

	[ClientRpc(Delivery = RpcDelivery.Reliable)]
	private void UpdateTurnClientRpc(int active)
	{
		if (!IsServer)
			hud.SetPlayerTurn(active);
	}

	#endregion
}
