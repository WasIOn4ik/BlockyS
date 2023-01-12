using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
using UnityEngine.SceneManagement;

[Serializable]
public struct PlayerCosmetic : IEquatable<PlayerCosmetic>, INetworkSerializable
{
    public int pawnSkinID;
    public int boardSkinID;

    public bool Equals(PlayerCosmetic other)
    {
        return pawnSkinID == other.pawnSkinID && boardSkinID == other.boardSkinID;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref pawnSkinID);
        serializer.SerializeValue(ref boardSkinID);
    }
}

public class GameplayBase : NetworkBehaviour
{
    #region Variables

    [Header("Components")]
    [SerializeField] protected SinglePlayerController singleControllerPrefab;

    [SerializeField] protected NetworkPlayerController networkControllerPrefab;

    [SerializeField] protected BoardWall wallPrefab;
    [SerializeField] protected Pawn pawnPrefab;

    public Gameboard gameboard = new Gameboard();

    public InGameHUD hud;

    [Header("Gameplay")]
    [Tooltip("X и Z умножаются на halfExtent, а Y остается без изменений")]
    [SerializeField] protected List<Vector3> playersStartPositions = new();

    [SerializeField] protected List<Vector3> playersStartRotation = new();

    public bool bGameActive = false;

    public SpesAnimator cameraAnimator = new();

    protected List<IPlayerController> S_players = new();

    public NetworkVariable<int> ActivePlayer { get; protected set; } = new NetworkVariable<int>();

    public NetworkList<NetworkBehaviourReference> C_pawns = new NetworkList<NetworkBehaviourReference>();

    protected Dictionary<int, ulong> ordersToNetIDs = new();

    protected MenuBase waitingMenu;
    #endregion

    #region StaticVariables

    public static GameplayBase instance;

    #endregion

    #region UnityCallbakcs

    public void Awake()
    {
        if (instance)
            Destroy(this);

        instance = this;

        NetworkManager.OnClientConnectedCallback += OnClientConnected;
    }

    public override void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        NetworkManager.OnClientConnectedCallback -= OnClientConnected;

        base.OnDestroy();
    }

    #endregion

    #region NetworkCallbakcs

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        SpesLogger.Detail("GmplB: networkSpawn " + (IsServer ? "{Сервер}" : "{Клиент}"));

        if (IsServer)
        {
            gameboard.Initialize(GameBase.server.prefs.boardHalfExtent);

            for (int i = 0; i < GameBase.server.localPlayers; i++)
            {
                ordersToNetIDs.Add(i, NetworkManager.Singleton.LocalClientId);
                var player = S_SpawnAbstractPlayer<SinglePlayerController>(singleControllerPrefab);
            }
            S_HandleWaitingMenu();
            ActivePlayer.Value = 0;
        }
        else if (IsClient)
        {
            RequestInitializeServerRpc();
        }
        ActivePlayer.OnValueChanged += OnActivePlayerChanged;
    }

    private void OnActivePlayerChanged(int previousValue, int newValue)
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
        SpesLogger.Detail("GmplB: Клиент " + clientID + " подключился");
        if (IsServer && clientID != OwnerClientId)
        {
            ordersToNetIDs.Add(S_players.Count, clientID);
            var player = S_SpawnAbstractPlayer<NetworkPlayerController>(networkControllerPrefab);
            player.NetworkObject.SpawnAsPlayerObject(clientID);
            player.cosmetic.OnValueChanged += S_UpdateSkins;
        }
        S_HandleWaitingMenu();
    }

    private void S_UpdateSkins(PlayerCosmetic previousValue, PlayerCosmetic newValue)
    {
        SpesLogger.Deb("S_UpdateSkins");
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
    /// SERVER-FUNCTION: Вызывается сервером у локальной копии GameplayBase для завершения хода
    /// </summary>
    /// <param name="controller"></param>
    /// <param name="turn"></param>
    public void S_EndTurn(IPlayerController controller, Turn turn)
    {
        if (ActivePlayer.Value != controller.GetPlayerInfo().playerOrder)
        {
            SpesLogger.Warning("Получен ход от игрока" + controller.GetPlayerInfo().playerOrder + " но сейчас не его ход");
            return;
        }

        switch (turn.type)
        {
            case ETurnType.Move:
                var pawn = S_players[ActivePlayer.Value].GetPlayerInfo().pawn;

                if (!CheckMove(pawn, turn))
                {
                    SpesLogger.Warning("Был совершен некорректный ход при движении пешки, ход возвращен игроку" + controller.GetPlayerInfo().playerOrder);
                    controller.StartTurn();
                    return;
                }

                var block = gameboard.blocks[turn.pos.x, turn.pos.y];

                pawn.block.Value = block.coords;

                if (pawn.block.Value.x == GameBase.server.prefs.boardHalfExtent && pawn.block.Value.y == GameBase.server.prefs.boardHalfExtent)
                {
                    SpesLogger.Detail("Игра завершена, пешка входит в финальную клетку");
                    CancelInvoke("OnTimeout");
                    return;
                }
                break;

            case ETurnType.PlaceXForward:
                if (!CheckPlace(turn))
                {
                    SpesLogger.Warning("Был совершен некорректный ход при создании стенки, ход возвращен игроку" + controller.GetPlayerInfo().playerOrder);
                    controller.StartTurn();
                    return;
                }
                var infoX = controller.GetPlayerInfo();

                if (infoX.WallCount <= 0)
                {
                    SpesLogger.Warning("Игрок не может построить больше стен: " + controller.GetPlayerInfo().playerOrder);
                    controller.StartTurn();
                    return;
                }
                var wphX = gameboard.wallsPlaces[turn.pos.x, turn.pos.y];

                var wallX = Instantiate(wallPrefab, wphX.transform.position, Quaternion.Euler(0, 0, 0), GameplayBase.instance.transform);
                wallX.NetworkObject.Spawn();
                wallX.coords.Value = turn;
                wallX.OnAnimated += cameraAnimator.AnimateCamera;

                infoX.WallCount -= 1;
                controller.SetPlayerInfo(infoX);

                CancelInvoke("OnTimeout");

                break;
            case ETurnType.PlaceZForward:
                if (!CheckPlace(turn))
                {
                    SpesLogger.Warning("Был совершен некорректный ход при создании стенки, ход возвращен игроку" + controller.GetPlayerInfo().playerOrder);
                    controller.StartTurn();
                    return;
                }
                var infoZ = controller.GetPlayerInfo();

                if (infoZ.WallCount <= 0)
                {
                    SpesLogger.Warning("Игрок не может построить больше стен: " + controller.GetPlayerInfo().playerOrder);
                    controller.StartTurn();
                    return;
                }
                var wphZ = gameboard.wallsPlaces[turn.pos.x, turn.pos.y];

                var wallZ = Instantiate(wallPrefab, wphZ.transform.position, Quaternion.Euler(0, 90, 0), GameplayBase.instance.transform);
                wallZ.NetworkObject.Spawn();
                wallZ.coords.Value = turn;
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

        //Если не сработал return, то ход передается следующеу игроку в следующем кадре
        ActivePlayer.Value++;
        if (ActivePlayer.Value >= GameBase.server.prefs.maxPlayers)
        {
            ActivePlayer.Value = 0;
        }

        CancelInvoke("OnTimeout");

        StartCoroutine(nextTurn());
    }

    /// <summary>
    /// Проверяет возможность передвижения пешки в определенную позицию
    /// </summary>
    /// <param name="pawn"></param>
    /// <param name="turn"></param>
    /// <returns></returns>
    public bool CheckMove(Pawn pawn, Turn turn)
    {
        int x = turn.pos.x;
        int y = turn.pos.y;

        int curX = pawn.block.Value.x;
        int curY = pawn.block.Value.y;

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

    public bool CheckDestination(Turn turn)
    {
        foreach (var pl in S_players)
        {
            var pawn = pl.GetPlayerInfo().pawn;

            if (!gameboard.HasPath(pawn.block.Value, turn.pos, turn.type))
            {
                SpesLogger.Deb("Не найден путь от игрока" + pl.GetPlayerInfo().playerOrder + " до финиша");
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Передает ход активному игроку в следующем Tick'е
    /// </summary>
    /// <returns></returns>
    protected IEnumerator nextTurn()
    {
        yield return new WaitForEndOfFrame();
        yield return null;
        cameraAnimator.controller = S_players[ActivePlayer.Value];
        S_players[ActivePlayer.Value].StartTurn();
        S_UpdatePlayersTurn();
        SpesLogger.Detail("Ход передается игроку: " + ActivePlayer.Value);
        Invoke("OnTimeout", GameBase.instance.gameRules.turnTime + 1);
    }

    protected void OnTimeout()
    {
        var info = S_players[ActivePlayer.Value].GetPlayerInfo();
        info.state = EPlayerState.Operator;
        S_players[ActivePlayer.Value].SetPlayerInfo(info);
        ActivePlayer.Value++;
        if (ActivePlayer.Value >= GameBase.server.prefs.maxPlayers)
        {
            ActivePlayer.Value = 0;
        }
        StartCoroutine(nextTurn());
        StartCoroutine(check());
    }
    protected IEnumerator check()
    {
        yield return new WaitForEndOfFrame();
        yield return null;
        var p = S_players[ActivePlayer.Value].GetPlayerInfo().pawn.block.Value;
        S_players[ActivePlayer.Value].GetPlayerInfo().pawn.HandleAnimation(GameplayBase.instance.gameboard.blocks[p.x, p.y]);
    }

    /// <summary>
    /// Преобразует координаты стартовой позиции Vector3 из x,y,z[0:1] в Point x,y[0:halfExt*2]
    /// </summary>
    /// <param name="vec"></param>
    /// <returns></returns>
    protected Point PreselectedPoint(Vector3 vec)
    {
        Point p = new Point();
        p.x = GameBase.server.prefs.boardHalfExtent * (1 + (int)vec.x);
        p.y = GameBase.server.prefs.boardHalfExtent * (1 + (int)vec.z);

        return p;
    }

    /// <summary>
    /// SERVER-FUNCTION: Создает контроллер игрока.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="prefab"></param>
    /// <returns></returns>
    protected T S_SpawnAbstractPlayer<T>(IPlayerController prefab) where T : MonoBehaviour, IPlayerController
    {
        int playerOrder = S_players.Count;

        Vector3 playerStartPosition = playersStartPositions[playerOrder];
        float y = playerStartPosition.y;
        playerStartPosition *= GameBase.server.prefs.boardHalfExtent;
        playerStartPosition.y = y;

        var player = Instantiate(prefab.GetMono(), playerStartPosition, Quaternion.Euler(playersStartRotation[playerOrder])) as T;
        player.name = "Controller_" + playerOrder;
        S_players.Add(player);

        var point = PreselectedPoint(playersStartPositions[playerOrder]);

        var info = player.GetPlayerInfo();
        info.playerOrder = playerOrder;
        info.pawn = S_SpawnPawn(playerOrder, gameboard.blocks[point.x, point.y]);
        //info.pawn.OnAnimated += cameraAnimator.AnimateCamera;
        int wallsCount = 5;
        switch (GameBase.server.prefs.boardHalfExtent)
        {
            case 5:
                wallsCount = GameBase.instance.gameRules.x5Count;
                break;

            case 7:
                wallsCount = GameBase.instance.gameRules.x7Count;
                break;

            case 9:
                wallsCount = GameBase.instance.gameRules.x7Count;
                break;
        }
        info.WallCount = wallsCount;
        info.state = EPlayerState.Waiting;

        player.SetPlayerInfo(info);

        return player as T;
    }

    /// <summary>
    /// SERVER-FUNCTION: Создает пешку игрока, спавнит в мире, регистрирует у игрока
    /// </summary>
    /// <param name="playerOrder"></param>
    /// <param name="block"></param>
    /// <returns></returns>
    protected Pawn S_SpawnPawn(int playerOrder, BoardBlock block)
    {
        Pawn newPawn = Instantiate(pawnPrefab, block.transform.position, Quaternion.Euler(0, playersStartRotation[playerOrder].y, 0));
        newPawn.name = "Pawn_" + playerOrder;

        newPawn.NetworkObject.SpawnWithOwnership(ordersToNetIDs[playerOrder]);

        var info = S_players[playerOrder].GetPlayerInfo();
        info.pawn = newPawn;
        S_players[playerOrder].SetPlayerInfo(info);

        newPawn.block.Value = block.coords;
        newPawn.playerOrder.Value = playerOrder;
        C_pawns.Add(newPawn);
        return newPawn;
    }

    /// <summary>
    /// Убирает окно ожидания, когда все игроки подключились, передает ход нулевому игроку и обновляет флаг bGameActive на true, сообщает игрокам чей сейчас ход
    /// </summary>
    protected void S_HandleWaitingMenu()
    {
        if (IsServer)
        {
            if (S_players.Count == GameBase.server.prefs.maxPlayers)
            {
                SpesLogger.Deb("S_HandleWaitingMenu");
                ShowWaitingScreenClientRpc(false);
                UpdateSkinsClientRpc(GetCosmetics());
                cameraAnimator.controller = S_players[0];
                S_players[0].StartTurn();
                Invoke("OnTimeout", GameBase.instance.gameRules.turnTime + 1);
                bGameActive = true;
                S_UpdatePlayersTurn();
            }
            else
            {
                ShowWaitingScreenClientRpc(true);
            }
        }
    }

    protected PlayerCosmetic[] GetCosmetics()
    {
        List<PlayerCosmetic> list = new();
        foreach (var pl in S_players)
        {
            list.Add(pl.GetCosmetic());
        }
        return list.ToArray();
    }

    protected void S_UpdatePlayersTurn()
    {
        foreach (var pl in S_players)
        {
            pl.UpdateTurn(ActivePlayer.Value);
        }
    }

    #endregion

    #region RPCs

    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    public void UpdateSkinsClientRpc(PlayerCosmetic[] skins)
    {
        string title = "";
        foreach (var s in skins)
        {
            title += GameBase.instance.skins.boardSkins[s.boardSkinID].name + " ";
        }
        SpesLogger.Deb("USClientRpc update: " + title);

        //Обновление карты
        gameboard.UpdateSkins(skins);

        //Обработка скинов пешек
        if (IsServer)
        {
            for (int i = 0; i < skins.Length; i++)
            {
                S_players[i].GetPlayerInfo().pawn.SetSkinClientRpc(skins[i].pawnSkinID);
            }
        }

    }

    [ServerRpc(Delivery = RpcDelivery.Reliable, RequireOwnership = false)]
    public void RequestInitializeServerRpc(ServerRpcParams param = default)
    {
        ClientRpcParams cParams = new();
        cParams.Send.TargetClientIds = new ulong[] { param.Receive.SenderClientId };
        InitializeClientRpc(GameBase.server.prefs.boardHalfExtent, cParams);
    }

    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    public void InitializeClientRpc(int halfExtent, ClientRpcParams param = default)
    {
        gameboard.Initialize(halfExtent);
    }

    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    public void ShowWaitingScreenClientRpc(bool bShow)
    {
        if (bShow)
        {
            waitingMenu = MenuBase.OpenMenu("WaitingMenu");
        }
        else
        {
            if (waitingMenu)
                Destroy(waitingMenu.gameObject);
        }
    }

    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    public void GameFinishedClientRpc(string winner)
    {
        SpesLogger.Detail("Победил игрок " + winner);

        string pureName = winner.Split("_")[0];

        int coinsValue = pureName == GameBase.client.playerName ? 100 : 25;

        GameBase.storage.progress.coins += coinsValue;

        ShowWinMessage(winner, coinsValue);

        if (IsServer)
            GameBase.server.Invoke("ClearAll", 5);
        else
            GameBase.client.ClearAll();

        //SceneManager.LoadScene("StartupScene");
    }

    protected void ShowWinMessage(string winner, int coinsValue)
    {

        LocalizedString winnerStr = new LocalizedString("Messages", "GameEnd")
                    {
                        {"winnerName", new StringVariable{Value = winner } },
                        {"gold", new IntVariable{Value =  coinsValue } }
                    };

        GameBase.instance.ShowMessage(winnerStr.GetLocalizedString(), MessageAction.LoadScene, false, "StartupScene");
    }
    #endregion
}
