using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SinglePlayerController : MonoBehaviour, IPlayerController
{
	#region Variables

	[SerializeField] private InGameHUD hudPrefab;

	[SerializeField] private PlayerInGameInfo playerInfo = new();

	[SerializeField] private PlayerCosmetic cosmetic;

	private Vector3 cameraPosition;
	private Quaternion cameraRotation;

	private InputComponent inputComp;

	private Camera cam;

	#endregion

	#region StaticVariables

	private static InGameHUD hud;
	private static SinglePlayerController previousLocalController;

	#endregion

	#region UnityCallbacks

	private void Awake()
	{
		playerInfo.state = EPlayerState.Operator;
		cameraPosition = transform.position;
		cameraRotation = transform.rotation;

		inputComp = GetComponent<InputComponent>();

		if (!hud)
			hud = Instantiate(hudPrefab);

		inputComp.turnValid += hud.OnTurnValidationChanged;
		inputComp.SetVectors(transform.forward, transform.right);

		cosmetic = new PlayerCosmetic() { boardSkinID = GameBase.Storage.CurrentBoardSkinID, pawnSkinID = GameBase.Storage.CurrentPawnSkinID };
	}

	#endregion

	#region IPlayerController

	public void TurnTimeout()
	{
		GetPlayerInfo().pawn.JumpOnSpot();
	}

	public void StartTurn()
	{
		SpesLogger.Deb($"Turn of local player {GetPlayerInfo().playerOrder} started");

		if (previousLocalController)
		{
			var linfo = previousLocalController.GetPlayerInfo();
			linfo.state = EPlayerState.Operator;
			previousLocalController.SetPlayerInfo(linfo);
			BoardBlock.ClearCurrentSelection();
		}
		previousLocalController = this;

		var info = GetPlayerInfo();
		info.state = EPlayerState.ActivePlayer;
		SetPlayerInfo(info);

		cam = Camera.main;
		var tp = cam.transform.position;
		var tr = cam.transform.rotation;

		cam.transform.SetParent(transform);

		//new camera position
		Vector3 pos = GetPlayerInfo().pawn.transform.position + GetPlayerInfo().pawn.transform.forward * GameBase.Instance.gameRules.cameraBackwardOffset + Vector3.up * GameBase.Instance.gameRules.cameraHeight;
		transform.SetPositionAndRotation(pos, cameraRotation);

		hud.SetInputComponent(inputComp);
		hud.ToDefault();
		hud.SetWallsCount(GetPlayerInfo().WallCount);
		inputComp.UpdateTurnValid(false);

		Debug.Log("State:" + GameplayBase.Instance.Stage.ToString());

		//Initialize camera on start and remove "camera jitter effect" on turn transfer
		if (GameplayBase.Instance.Stage == GameStage.GameActive)
		{
			cam.transform.position = tp;
			cam.transform.rotation = tr;
		}
		else
		{
			cam.transform.localPosition = Vector3.zero;
			cam.transform.localRotation = Quaternion.identity;
		}
	}

	public void EndTurn(Turn turn)
	{
		if (turn.type == ETurnType.PlaceXForward || turn.type == ETurnType.PlaceZForward)
		{
			if (GetPlayerInfo().WallCount <= 0)
			{
				SpesLogger.Detail($"Player {GetPlayerInfo().playerOrder} can't build anymore");
				return;
			}
		}
		SpesLogger.Deb($"Local player {GetPlayerInfo().playerOrder} ended turn");

		previousLocalController = this;

		var info = GetPlayerInfo();
		info.state = EPlayerState.Waiting;
		SetPlayerInfo(info);

		cameraPosition = transform.position;

		GameplayBase.Instance.S_EndTurn(this, turn);
	}

	public MonoBehaviour GetMono()
	{
		return this;
	}

	public PlayerInGameInfo GetPlayerInfo()
	{
		return playerInfo;
	}

	public void SetPlayerInfo(PlayerInGameInfo inf)
	{
		playerInfo = inf;
	}

	public PlayerCosmetic GetCosmetic()
	{
		return cosmetic;
	}

	public void UpdateTurn(int active)
	{
		hud.SetPlayerTurn(active);
	}

	#endregion
}

