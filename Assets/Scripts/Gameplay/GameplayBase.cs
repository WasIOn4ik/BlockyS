using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.SmartFormat.PersistentVariables;

public enum GameStage
{
	WaitingForPlayersToConnect,
	WaitingForPlayersToLoad,
	GameActive,
	GameFinished
}
public class GameplayBase : NetworkBehaviour
{
	#region Variables

	public Gameboard gameboard = new Gameboard();

	public InGameHUD hud;

	public GameStage gameStage;

	public SpesAnimator cameraAnimator = new();

	public NetworkVariable<int> ActivePlayer { get; protected set; } = new NetworkVariable<int>();

	[Header("Properties")]

	public LocalizedString winnerStr = new LocalizedString("Messages", "GameEnd");
	[SerializeField] private string goldVariable = "gold";
	[SerializeField] private string winnerNameVariable = "winnerName";

	[SerializeField] private Vector3 zForwardRotation;
	[SerializeField] private Vector3 xForwardRotation;

	[Header("Components")]
	[SerializeField] private SinglePlayerController singleControllerPrefab;

	[SerializeField] private NetworkPlayerController networkControllerPrefab;

	[SerializeField] private BoardWall wallPrefab;
	[SerializeField] private Pawn pawnPrefab;

	[Header("Gameplay")]
	[Tooltip("X and Z are multiplied by halfExtent, Y will not be changed")]
	[SerializeField] private List<Vector3> playersStartPositions = new();

	[SerializeField] private List<Vector3> playersStartRotation = new();

	private NetworkList<NetworkBehaviourReference> C_pawns;

	private List<IPlayerController> S_players = new();

	private Dictionary<int, ulong> ordersToNetIDs = new();

	private MenuBase waitingMenu;

	#endregion

	#region StaticVariables

	public static GameplayBase Instance { get; private set; }

	#endregion

	#region UnityCallbakcs

	public void Awake()
	{
		if (Instance)
			Destroy(this);

		Instance = this;
		C_pawns = new NetworkList<NetworkBehaviourReference>();
		NetworkManager.OnClientConnectedCallback += OnClientConnected;
	}

	public override void OnDestroy()
	{
		if (Instance == this)
		{
			Instance = null;
		}

		if (NetworkManager)
			NetworkManager.OnClientConnectedCallback -= OnClientConnected;

		base.OnDestroy();
	}

	#endregion

	#region NetworkCallbacks

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();

		SpesLogger.Detail("GmplB: networkSpawn " + (IsServer ? "{Server}" : "{Client}"));

		if (IsServer)
		{
			gameboard.Initialize(GameBase.Server.prefs.boardHalfExtent);

			HandleLocalPlayers();

			S_HandleWaitingMenu();
			ActivePlayer.Value = 0;
		}
		else if (IsClient)
		{
			RequestInitializeServerRpc();
		}
		ActivePlayer.OnValueChanged += ActivePlayer_OnValueChanged;
	}

	private void HandleLocalPlayers()
	{
		for (int i = 0; i < GameBase.Server.localPlayers; i++)
		{
			ordersToNetIDs.Add(i, NetworkManager.Singleton.LocalClientId);
			S_SpawnAbstractPlayer<SinglePlayerController>(singleControllerPrefab);
		}
	}

	private void ActivePlayer_OnValueChanged(int previousValue, int newValue)
	{
		foreach (var p in C_pawns)
		{
			if (p.TryGet(out Pawn pawn))
			{
				pawn.UpdateColor();
			}
		}
	}

	private void OnClientConnected(ulong clientID)
	{
		SpesLogger.Detail("GmplB: Client " + clientID + " connected");
		if (IsServer && clientID != OwnerClientId)
		{
			ordersToNetIDs.Add(S_players.Count, clientID);
			var player = S_SpawnAbstractPlayer<NetworkPlayerController>(networkControllerPrefab);
			player.NetworkObject.SpawnAsPlayerObject(clientID);
			player.onNetworkCosmeticChanged += Cosmetic_OnValueChanged;
		}
		S_HandleWaitingMenu();
	}

	private void Cosmetic_OnValueChanged(object sender, EventArgs e)
	{
		UpdateSkinsClientRpc(GetCosmetics());
	}

	#endregion

	#region Functions

	public void UpdateDefaultSkinColorForPawns()
	{
		foreach (var el in S_players)
		{
			if (el != null)
			{
				el.GetPlayerInfo().pawn.UpdateColor();
			}
		}
	}

	/// <summary>
	/// SERVER-FUNCTION: Call it from server to invoke local EndTurn
	/// </summary>
	/// <param name="controller"></param>
	/// <param name="turn"></param>
	public void S_EndTurn(IPlayerController controller, Turn turn)
	{
		if (ActivePlayer.Value != controller.GetPlayerInfo().playerOrder)
		{
			SpesLogger.Warning("Received EndTurn from player" + controller.GetPlayerInfo().playerOrder + " but it's not active now");
			return;
		}

		switch (turn.type)
		{
			case ETurnType.Move:
				var pawn = S_players[ActivePlayer.Value].GetPlayerInfo().pawn;

				if (!CheckMove(pawn, turn))
				{
					SpesLogger.Warning("Turn was incorrect, aborting " + controller.GetPlayerInfo().playerOrder);
					controller.StartTurn();
					return;
				}

				var block = gameboard.blocks[turn.pos.x, turn.pos.y];

				pawn.Block = block.coords;

				if (pawn.Block.x == GameBase.Server.prefs.boardHalfExtent && pawn.Block.y == GameBase.Server.prefs.boardHalfExtent)
				{
					SpesLogger.Detail("Game ended, final block reached");
					CancelInvoke("OnTimeout");
					return;
				}
				break;

			case ETurnType.PlaceXForward:
				if (!CheckPlace(turn))
				{
					SpesLogger.Warning("Incorrect wallplace turn " + controller.GetPlayerInfo().playerOrder);
					controller.StartTurn();
					return;
				}
				var infoX = controller.GetPlayerInfo();

				if (infoX.WallCount <= 0)
				{
					SpesLogger.Warning("Player can't place more walls: " + controller.GetPlayerInfo().playerOrder);
					controller.StartTurn();
					return;
				}
				var wphX = gameboard.wallsPlaces[turn.pos.x, turn.pos.y];

				var wallX = Instantiate(wallPrefab, wphX.transform.position, Quaternion.Euler(xForwardRotation), GameplayBase.Instance.transform);
				wallX.NetworkObject.Spawn();
				wallX.TurnInfo = turn;
				wallX.OnAnimated += cameraAnimator.AnimateCamera;

				infoX.WallCount -= 1;
				controller.SetPlayerInfo(infoX);

				CancelInvoke("OnTimeout");

				break;
			case ETurnType.PlaceZForward:
				if (!CheckPlace(turn))
				{
					SpesLogger.Warning("Incorrect wallplace turn " + controller.GetPlayerInfo().playerOrder);
					controller.StartTurn();
					return;
				}
				var infoZ = controller.GetPlayerInfo();

				if (infoZ.WallCount <= 0)
				{
					SpesLogger.Warning("Player can't place more walls: " + controller.GetPlayerInfo().playerOrder);
					controller.StartTurn();
					return;
				}
				var wphZ = gameboard.wallsPlaces[turn.pos.x, turn.pos.y];

				var wallZ = Instantiate(wallPrefab, wphZ.transform.position, Quaternion.Euler(zForwardRotation), GameplayBase.Instance.transform);
				wallZ.NetworkObject.Spawn();
				wallZ.TurnInfo = turn;
				wallZ.OnAnimated += cameraAnimator.AnimateCamera;

				infoZ.WallCount -= 1;
				controller.SetPlayerInfo(infoZ);

				CancelInvoke("OnTimeout");

				break;
			case ETurnType.DestroyXWall:
				break;
			case ETurnType.DestroyZWall:
				break;
		}

		//If not returned yet,transfer turn to the next player in the next frame
		ActivePlayer.Value++;
		if (ActivePlayer.Value >= GameBase.Server.prefs.maxPlayers)
		{
			ActivePlayer.Value = 0;
		}

		CancelInvoke("OnTimeout");

		StartCoroutine(nextTurn());
	}

	public bool CheckPlace(Turn turn)
	{
		if (!gameboard.wallsPlaces[turn.pos.x, turn.pos.y].bEmpty)
			return false;

		switch (turn.type)
		{
			case ETurnType.PlaceXForward:
				if (gameboard.blocks[turn.pos.x, turn.pos.y].zDir && gameboard.blocks[turn.pos.x + 1, turn.pos.y].zDir)
				{
					return CheckDestination(turn);
				}
				break;
			case ETurnType.PlaceZForward:
				if (gameboard.blocks[turn.pos.x, turn.pos.y].xDir && gameboard.blocks[turn.pos.x, turn.pos.y + 1].xDir)
				{
					return CheckDestination(turn);
				}
				break;
		}
		return false;

	}

	/// <summary>
	/// Checks availability of pawn placing
	/// </summary>
	/// <param name="pawn"></param>
	/// <param name="turn"></param>
	/// <returns></returns>
	private bool CheckMove(Pawn pawn, Turn turn)
	{
		int x = turn.pos.x;
		int y = turn.pos.y;

		int curX = pawn.Block.x;
		int curY = pawn.Block.y;

		var block = gameboard.blocks[curX, curY];

		if (!gameboard.blocks[x, y].bEmpty)
			return false;

		if (Mathf.Abs(x - curX) + Mathf.Abs(y - curY) > 1)
			return false;

		if (x > curX)
		{
			return block.xDir;
		}
		else if (x < curX)
		{
			return block.mxDir;
		}
		else if (y > curY)
		{
			return block.zDir;
		}
		else if (y < curY)
		{
			return block.mzDir;
		}
		return false;
	}

	private bool CheckDestination(Turn turn)
	{
		foreach (var pl in S_players)
		{
			var pawn = pl.GetPlayerInfo().pawn;

			if (!gameboard.HasPath(pawn.Block, turn.pos, turn.type))
			{
				SpesLogger.Deb($"Player {pl.GetPlayerInfo().playerOrder} has no path to finish");
				return false;
			}
		}
		return true;
	}

	/// <summary>
	/// Transfer turn to the next player
	/// </summary>
	/// <returns></returns>
	private IEnumerator nextTurn()
	{
		yield return new WaitForEndOfFrame();
		yield return null;
		S_players[ActivePlayer.Value].StartTurn();
		S_UpdatePlayersTurn();
		SpesLogger.Detail("Turn transfered to player: " + ActivePlayer.Value);
		Invoke("OnTimeout", GameBase.Instance.gameRules.turnTime + 1);
	}

	private void OnTimeout()
	{
		var info = S_players[ActivePlayer.Value].GetPlayerInfo();
		info.state = EPlayerState.Operator;
		S_players[ActivePlayer.Value].SetPlayerInfo(info);
		ActivePlayer.Value++;
		if (ActivePlayer.Value >= GameBase.Server.prefs.maxPlayers)
		{
			ActivePlayer.Value = 0;
		}
		StartCoroutine(nextTurn());
		StartCoroutine(check());
	}

	private IEnumerator check()
	{
		yield return new WaitForEndOfFrame();
		yield return null;
		var p = S_players[ActivePlayer.Value].GetPlayerInfo().pawn.Block;
		S_players[ActivePlayer.Value].GetPlayerInfo().pawn.HandleAnimation(GameplayBase.Instance.gameboard.blocks[p.x, p.y]);
	}

	/// <summary>
	/// Translates Vector3 from x,y,z[0:1] to Point x,y[0:halfExt*2]
	/// </summary>
	/// <param name="vec"></param>
	/// <returns></returns>
	private Point PreselectedPoint(Vector3 vec)
	{
		Point p = new Point();
		p.x = GameBase.Server.prefs.boardHalfExtent * (1 + (int)vec.x);
		p.y = GameBase.Server.prefs.boardHalfExtent * (1 + (int)vec.z);

		return p;
	}

	/// <summary>
	/// SERVER-FUNCTION: Creates player controller
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="prefab"></param>
	/// <returns></returns>
	private T S_SpawnAbstractPlayer<T>(IPlayerController prefab) where T : MonoBehaviour, IPlayerController
	{
		int playerOrder = S_players.Count;

		Vector3 playerStartPosition = playersStartPositions[playerOrder];
		float y = playerStartPosition.y;
		playerStartPosition *= GameBase.Server.prefs.boardHalfExtent;
		playerStartPosition.y = y;

		var player = Instantiate(prefab.GetMono(), playerStartPosition, Quaternion.Euler(playersStartRotation[playerOrder])) as T;
		player.name = "Controller_" + playerOrder;
		ActivePlayer.OnValueChanged += (oldVal, newVal) =>
		{
			player.UpdateTurn(newVal);
		};
		S_players.Add(player);

		var point = PreselectedPoint(playersStartPositions[playerOrder]);

		var info = player.GetPlayerInfo();
		info.playerOrder = playerOrder;
		info.pawn = S_SpawnPawn(playerOrder, gameboard.blocks[point.x, point.y]);
		//info.pawn.OnAnimated += cameraAnimator.AnimateCamera;
		int wallsCount = 5;
		switch (GameBase.Server.prefs.boardHalfExtent)
		{
			case 5:
				wallsCount = GameBase.Instance.gameRules.x5Count;
				break;

			case 7:
				wallsCount = GameBase.Instance.gameRules.x7Count;
				break;

			case 9:
				wallsCount = GameBase.Instance.gameRules.x7Count;
				break;
		}
		info.WallCount = wallsCount;
		info.state = EPlayerState.Waiting;

		player.SetPlayerInfo(info);

		return player as T;
	}

	/// <summary>
	/// SERVER-FUNCTION: Creates player pawn, spawns it in world and registers it
	/// </summary>
	/// <param name="playerOrder"></param>
	/// <param name="block"></param>
	/// <returns></returns>
	private Pawn S_SpawnPawn(int playerOrder, BoardBlock block)
	{
		Pawn newPawn = Instantiate(pawnPrefab, block.transform.position, Quaternion.Euler(0, playersStartRotation[playerOrder].y, 0));
		newPawn.name = "Pawn_" + playerOrder;

		newPawn.NetworkObject.SpawnWithOwnership(ordersToNetIDs[playerOrder]);

		var info = S_players[playerOrder].GetPlayerInfo();
		info.pawn = newPawn;
		S_players[playerOrder].SetPlayerInfo(info);

		newPawn.Block = block.coords;
		newPawn.PlayerOrder = playerOrder;
		C_pawns.Add(newPawn);
		return newPawn;
	}

	/// <summary>
	/// Hides waiting screen, when all players connected transfers turn to zero player and updates bGameActive to true, messages players about current turn
	/// </summary>
	private void S_HandleWaitingMenu()
	{
		if (IsServer)
		{
			if (S_players.Count == GameBase.Server.prefs.maxPlayers)
			{
				ShowWaitingScreenClientRpc(false);
				UpdateSkinsClientRpc(GetCosmetics());
				S_players[0].StartTurn();
				Invoke("OnTimeout", GameBase.Instance.gameRules.turnTime + 1);
				gameStage = GameStage.GameActive;
			}
			else
			{
				ShowWaitingScreenClientRpc(true);
			}
		}
	}

	private PlayerCosmetic[] GetCosmetics()
	{
		List<PlayerCosmetic> list = new();
		foreach (var pl in S_players)
		{
			list.Add(pl.GetCosmetic());
		}
		return list.ToArray();
	}

	private void S_UpdatePlayersTurn()
	{
		foreach (var pl in S_players)
		{
			pl.UpdateTurn(ActivePlayer.Value);
		}
	}

	private void ShowWinMessage(string winner, int coinsValue)
	{
		winnerStr.Add(winnerNameVariable, new StringVariable { Value = winner });
		winnerStr.Add(goldVariable, new IntVariable { Value = coinsValue });

		GameBase.Instance.ShowMessage(winnerStr.GetLocalizedString(), MessageAction.LoadScene, false, "StartupScene");
	}

	#endregion

	#region RPCs


	[ClientRpc(Delivery = RpcDelivery.Reliable)]
	public void GameFinishedClientRpc(string winner)
	{
		SpesLogger.Detail($"Player {winner} wins");

		string pureName = winner.Split("_")[0];

		int coinsValue = pureName == GameBase.Client.playerName ? 100 : 25;

		GameBase.Storage.progress.coins += coinsValue;

		ShowWinMessage(winner, coinsValue);

		if (IsServer)
			GameBase.Server.Invoke("ClearAll", 5);
		else
			GameBase.Client.ClearAll();
	}

	[ClientRpc(Delivery = RpcDelivery.Reliable)]
	private void UpdateSkinsClientRpc(PlayerCosmetic[] skins)
	{
		string title = "";
		foreach (var s in skins)
		{
			title += GameBase.Instance.skins.GetBoard(s.boardSkinID).name + " ";
		}
		SpesLogger.Deb("USClientRpc update: " + title);

		GameBase.Instance.skins.PreloadBoardSkins(skins, () => { gameboard.UpdateSkins(skins); });

		//Pawn skins update
		if (IsServer)
		{
			for (int i = 0; i < skins.Length; i++)
			{
				S_players[i].GetPlayerInfo().pawn.SetSkinClientRpc(skins[i].pawnSkinID);
			}
		}

	}

	[ServerRpc(Delivery = RpcDelivery.Reliable, RequireOwnership = false)]
	private void RequestInitializeServerRpc(ServerRpcParams param = default)
	{
		ClientRpcParams cParams = new();
		cParams.Send.TargetClientIds = new ulong[] { param.Receive.SenderClientId };
		InitializeClientRpc(GameBase.Server.prefs.boardHalfExtent, cParams);
	}

	[ClientRpc(Delivery = RpcDelivery.Reliable)]
	private void InitializeClientRpc(int halfExtent, ClientRpcParams param = default)
	{
		gameboard.Initialize(halfExtent);
	}

	[ClientRpc(Delivery = RpcDelivery.Reliable)]
	private void ShowWaitingScreenClientRpc(bool bShow)
	{
		if (bShow)
		{
			MenuBase.OpenMenu("WaitingMenu", x => { waitingMenu = x; });
		}
		else
		{
			if (waitingMenu)
				Destroy(waitingMenu.gameObject);
		}
	}

	#endregion
}
