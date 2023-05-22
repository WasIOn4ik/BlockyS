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
	#region ClientVariables

	public Gameboard gameboard = new Gameboard();

	public InGameHUD hud;

	#endregion

	#region NetworkVariables

	private NetworkList<int> boardSkins;

	private NetworkVariable<GameStage> gameStage = new NetworkVariable<GameStage>();

	private NetworkVariable<int> activePlayer = new NetworkVariable<int>();

	private NetworkVariable<int> halfExtent = new NetworkVariable<int>();

	public GameStage Stage { get { return gameStage.Value; } private set { gameStage.Value = value; } }

	#endregion

	#region GeneralVariables

	[Header("Properties")]

	public LocalizedString winnerStr = new LocalizedString("Messages", "GameEnd");
	[SerializeField] private string goldVariable = "gold";
	[SerializeField] private string winnerNameVariable = "winnerName";
	[SerializeField] private int winGoldAmount = 100;
	[SerializeField] private int loseGoldAmount = 25;

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

	private WaitingMenuUI waitingMenu;

	private List<ulong> readyPlayersClientIDs = new List<ulong>();

	#endregion

	#region StaticVariables

	public static GameplayBase Instance { get; private set; }

	#endregion

	#region UnityCallbakcs

	public void Awake()
	{
		if (Instance)
			Destroy(this);

		boardSkins = new NetworkList<int>();

		Instance = this;

		MenuBase.OpenMenu(MenuBase.WAITING_MENU, x =>
		{
			waitingMenu = x as WaitingMenuUI;
			waitingMenu.SetState(WaitingState.Loading);
		});
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

		gameStage.OnValueChanged += GameStage_OnValueChanged;

		if (IsServer)
		{
			NetworkManager.SceneManager.OnLoadEventCompleted += SceneManager_OnLoadEventCompleted;
			activePlayer.OnValueChanged += ActivePlayer_OnValueChanged;

			halfExtent.Value = GameBase.Server.GetGamePrefs().boardHalfExtent;
			boardSkins.Clear();
			gameStage.Value = GameStage.WaitingForPlayersToLoad;

			List<int> skinsToPreload = new List<int>();

			foreach (var p in GameBase.Server.players)
			{
				skinsToPreload.Add(p.boardSkinID);
				boardSkins.Add(p.boardSkinID);
			}

			gameboard.Initialize(halfExtent.Value);

			GameBase.Instance.skins.PreloadBoardSkins(skinsToPreload, () =>
			{
				foreach (var s in skinsToPreload)
				{
					if (!GameBase.Instance.skins.GetBoard(s).TryGetBlock(out var block))
						Debug.Log("Can't get block for skin " + s);
				}
				gameboard.UpdateSkins(skinsToPreload);
				waitingMenu.SetState(WaitingState.WaitingOtherPlayers);

				ReadyServerRpc();
			});

			activePlayer.Value = 0;
		}
		else if (IsClient)
		{
			gameboard.Initialize(halfExtent.Value);

			var skins = GetBoardSkinsList();
			GameBase.Instance.skins.PreloadBoardSkins(skins, () =>
			{
				waitingMenu.SetState(WaitingState.WaitingOtherPlayers);
				gameboard.UpdateSkins(skins);
				ReadyServerRpc();
			});
		}
	}

	private void SceneManager_OnLoadEventCompleted(string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
	{
		S_SpawnControllers();
	}

	private void GameStage_OnValueChanged(GameStage previousValue, GameStage newValue)
	{
		if (newValue == GameStage.GameActive)
		{
			waitingMenu.HideMenu();
		}
	}

	private void ActivePlayer_OnValueChanged(int previousValue, int newValue)
	{
		S_StartPlayerTurn(newValue);
	}

	private void OnClientConnected(ulong clientID)
	{
		SpesLogger.Detail("GmplB: Client " + clientID + " connected");
		if (IsServer && clientID != NetworkManager.ServerClientId)
		{
			var playerDescriptor = GameBase.Server.GetRemotePlayerByClientID(clientID);
			var player = S_SpawnAbstractPlayer<NetworkPlayerController>(networkControllerPrefab, playerDescriptor.playerOrder);
			player.NetworkObject.SpawnAsPlayerObject(clientID);
		}
	}

	#endregion

	#region Functions

	/// <summary>
	/// SERVER-FUNCTION: Call it from server to invoke local EndTurn
	/// </summary>
	/// <param name="controller"></param>
	/// <param name="turn"></param>
	public void S_EndTurn(IPlayerController controller, Turn turn)
	{
		if (activePlayer.Value != controller.GetPlayerInfo().playerOrder)
		{
			SpesLogger.Warning("Received EndTurn from player" + controller.GetPlayerInfo().playerOrder + " but it's not active now");
			return;
		}

		switch (turn.type)
		{
			case ETurnType.Move:
				var pawn = GameBase.Server.GetPlayerByOrder(activePlayer.Value).playerPawn;

				if (!CheckMove(pawn, turn))
				{
					SpesLogger.Warning("Turn was incorrect, aborting " + controller.GetPlayerInfo().playerOrder);
					controller.StartTurn();
					return;
				}

				var block = gameboard.blocks[turn.pos.x, turn.pos.y];

				pawn.Block = block.coords;

				if (pawn.Block.x == GameBase.Server.GetGamePrefs().boardHalfExtent && pawn.Block.y == GameBase.Server.GetGamePrefs().boardHalfExtent)
				{
					SpesLogger.Detail("Game ended, final block reached");
					CancelInvoke("OnTimeout");
					gameStage.Value = GameStage.GameFinished;
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

				var wallX = Instantiate(wallPrefab, wphX.transform.position, Quaternion.Euler(xForwardRotation), transform);
				wallX.NetworkObject.Spawn();
				wallX.TurnInfo = turn;
				wallX.OnAnimated += CameraAnimator.AnimateCamera;

				infoX.WallCount -= 1;
				controller.SetPlayerInfo(infoX);
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
				wallZ.OnAnimated += CameraAnimator.AnimateCamera;

				infoZ.WallCount -= 1;
				controller.SetPlayerInfo(infoZ);
				break;
			case ETurnType.DestroyXWall:
				break;
			case ETurnType.DestroyZWall:
				break;
		}

		//If not returned yet,transfer turn to the next player in the next frame

		S_NextPlayerOrder();
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

	private void S_NextPlayerOrder()
	{
		if (!IsServer)
			return;

		int value = activePlayer.Value + 1;

		activePlayer.Value = value >= GameBase.Server.GetPlayersCount() ? 0 : value;
	}

	private void S_StartPlayerTurn(int playerOrder)
	{
		if (!IsServer)
			return;

		CancelInvoke("OnTimeout");
		var playerDescriptor = GameBase.Server.GetPlayerByOrder(activePlayer.Value);
		playerDescriptor.playerController.StartTurn();
		foreach (var pl in GameBase.Server.players)
		{
			pl.playerController.UpdateTurn(activePlayer.Value);
		}
		Invoke("OnTimeout", GameBase.Instance.gameRules.turnTime + 1f);
	}

	private void S_SpawnControllers()
	{
		if (!IsServer)
			return;

		foreach (var pl in GameBase.Server.players)
		{
			if (pl.bLocal)
			{
				S_SpawnAbstractPlayer<SinglePlayerController>(singleControllerPrefab, pl.playerOrder);
			}
			else
			{
				var controller = S_SpawnAbstractPlayer<NetworkPlayerController>(networkControllerPrefab, pl.playerOrder);
				controller.NetworkObject.SpawnWithOwnership(pl.clientID);
			}
		}
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
		foreach (var pl in GameBase.Server.players)
		{
			var pawn = pl.playerPawn;

			if (!gameboard.HasPath(pawn.Block, turn.pos, turn.type))
			{
				SpesLogger.Deb($"Player {pl.playerOrder} has no path to finish");
				return false;
			}
		}
		return true;
	}

	private void OnTimeout()
	{
		var controller = GameBase.Server.GetPlayerByOrder(activePlayer.Value).playerController;

		var info = controller.GetPlayerInfo();
		info.state = EPlayerState.Operator;
		controller.SetPlayerInfo(info);

		var pawn = GameBase.Server.GetPlayerByOrder(activePlayer.Value).playerPawn;

		controller.TurnTimeout();
		S_NextPlayerOrder();
	}

	/// <summary>
	/// Translates Vector3 from x,y,z[0:1] to Point x,y[0:halfExt*2]
	/// </summary>
	/// <param name="vec"></param>
	/// <returns></returns>
	private Point PreselectedPoint(Vector3 vec)
	{
		Point p = new Point();
		p.x = GameBase.Server.GetGamePrefs().boardHalfExtent * (1 + (int)vec.x);
		p.y = GameBase.Server.GetGamePrefs().boardHalfExtent * (1 + (int)vec.z);

		return p;
	}

	/// <summary>
	/// SERVER-FUNCTION: Creates player controller
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="prefab"></param>
	/// <returns></returns>
	private T S_SpawnAbstractPlayer<T>(IPlayerController prefab, int playerOrder) where T : MonoBehaviour, IPlayerController
	{
		int spawnID = GetSpawnPositionID(playerOrder);

		var player = InstantiateController(prefab, spawnID) as T;
		GameBase.Server.GetPlayerByOrder(playerOrder).playerController = player;
		player.name = "Controller_" + playerOrder;

		var point = PreselectedPoint(playersStartPositions[spawnID]);

		var info = player.GetPlayerInfo();
		info.playerOrder = playerOrder;
		info.pawn = S_SpawnPawn(playerOrder, gameboard.blocks[point.x, point.y], spawnID);
		//info.pawn.OnAnimated += cameraAnimator.AnimateCamera;
		int wallsCount = 5;
		switch (GameBase.Server.GetGamePrefs().boardHalfExtent)
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

	private MonoBehaviour InstantiateController(IPlayerController prefab, int spawnID)
	{
		return Instantiate(prefab.GetMono(), Vector3.zero, Quaternion.Euler(playersStartRotation[spawnID]));
	}

	/// <summary>
	/// Returns start position and rotation array index, based on max players count
	/// </summary>
	/// <param name="playerOrder"></param>
	/// <returns></returns>
	private int GetSpawnPositionID(int playerOrder)
	{

		int max = GameBase.Server.GetMaxPlayersCount();

		int id = playerOrder;

		if (max == 2)
			id = playerOrder * 2;

		return id;
	}

	/// <summary>
	/// SERVER-FUNCTION: Creates player pawn, spawns it in world and registers it
	/// </summary>
	/// <param name="playerOrder"></param>
	/// <param name="block"></param>
	/// <returns></returns>
	private Pawn S_SpawnPawn(int playerOrder, BoardBlock block, int spawnID)
	{
		var descriptor = GameBase.Server.GetPlayerByOrder(playerOrder);
		Pawn newPawn = Instantiate(pawnPrefab, block.transform.position, Quaternion.Euler(0, playersStartRotation[spawnID].y, 0));
		newPawn.name = "Pawn_" + playerOrder;

		newPawn.skinID.Value = descriptor.pawnSkinID;
		newPawn.NetworkObject.SpawnWithOwnership(descriptor.clientID);

		var playerDescriptor = GameBase.Server.GetPlayerByOrder(playerOrder);
		playerDescriptor.playerPawn = newPawn;

		var playerInfo = playerDescriptor.playerController.GetPlayerInfo();
		playerInfo.pawn = newPawn;
		playerDescriptor.playerController.SetPlayerInfo(playerInfo);

		newPawn.Block = block.coords;
		newPawn.PlayerOrder = playerOrder;
		return newPawn;
	}

	private List<int> GetBoardSkinsList()
	{
		List<int> list = new();
		foreach (var pl in boardSkins)
		{
			list.Add(pl);
		}

		return list;
	}

	private void ShowWinMessage(string winner, int coinsValue)
	{
		winnerStr.Add(winnerNameVariable, new StringVariable { Value = winner });
		winnerStr.Add(goldVariable, new IntVariable { Value = coinsValue });

		GameBase.Instance.ShowMessage(winnerStr.GetLocalizedString(), MessageAction.LoadScene, false, Scenes.StartupScene.ToString());
	}

	#endregion

	#region RPCs

	[ClientRpc(Delivery = RpcDelivery.Reliable)]
	public void GameFinishedClientRpc(string winner, ulong clientID)
	{
		string pureName = winner.Split("_")[0];

		int coinsValue = NetworkManager.Singleton.LocalClientId == clientID ? winGoldAmount : loseGoldAmount;

		GameBase.Storage.progress.coins += coinsValue;

		ShowWinMessage(winner, coinsValue);

		if (IsServer)
			GameBase.Server.Invoke("ClearAll", 5);
		else
			GameBase.Client.ClearAll();
	}

	[ServerRpc(RequireOwnership = false)]
	private void ReadyServerRpc(ServerRpcParams param = default)
	{

		readyPlayersClientIDs.Add(param.Receive.SenderClientId);

		SpesLogger.Detail($"Ready players: {readyPlayersClientIDs.Count} / {GameBase.Server.GetMaxRemotePlayersCount() + 1}");

		if (readyPlayersClientIDs.Count == GameBase.Server.GetMaxRemotePlayersCount() + 1)
		{
			gameStage.Value = GameStage.GameActive;

			S_StartPlayerTurn(activePlayer.Value);
		}
	}

	#endregion
}
