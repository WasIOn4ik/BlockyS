using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameplayBase : NetworkBehaviour
{
    #region Variables

    [Header("Components")]
    [SerializeField] protected SinglePlayerController singleControllerPrefab;

    [SerializeField] protected NetworkPlayerController networkControllerPrefab;

    [SerializeField] protected BoardWall wallPrefab;
    [SerializeField] protected Pawn pawnPrefab;

    public Gameboard gameboard = new Gameboard();

    [Header("Gameplay")]
    [Tooltip("X и Z умножаются на halfExtent, а Y остается без изменений")]
    [SerializeField] protected List<Vector3> playersStartPositions = new();

    [SerializeField] protected List<Vector3> playersStartRotation = new();

    [Range(0, 20)]
    [SerializeField] protected int wallsCount = 5;

    protected List<IPlayerController> players = new();

    protected int activePlayer = 0;

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

        base.OnDestroy();
    }

    #endregion

    #region NetworkCallbakcs

    private void OnClientConnected(ulong clientID)
    {
        SpesLogger.Detail("GmplB: Клиент " + clientID + " подключился");
        if (IsServer && clientID != OwnerClientId)
        {
            ordersToNetIDs.Add(players.Count, clientID);
            var player = S_SpawnAbstractPlayer<NetworkPlayerController>(networkControllerPrefab);
            player.NetworkObject.SpawnAsPlayerObject(clientID);
        }
        S_HandleWaitingMenu();
    }

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
        }
        else if (IsClient)
        {
            RequestInitializeServerRpc();
        }
    }

    #endregion

    #region Functions

    /// <summary>
    /// SERVER-FUNCTION: Вызывается сервером у локальной копии GameplayBase для завершения хода
    /// </summary>
    /// <param name="controller"></param>
    /// <param name="turn"></param>
    public void S_EndTurn(IPlayerController controller, Turn turn)
    {
        if (activePlayer != controller.GetPlayerInfo().playerOrder)
        {
            SpesLogger.Warning("Получен ход от игрока" + controller.GetPlayerInfo().playerOrder + " но сейчас не его ход");
            return;
        }

        switch (turn.type)
        {
            case ETurnType.Move:
                var pawn = players[activePlayer].GetPlayerInfo().pawn;

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
                var wph = gameboard.wallsPlaces[turn.pos.x, turn.pos.y];

                var wall = Instantiate(wallPrefab, wph.transform.position, Quaternion.Euler(0, 0, 0), GameplayBase.instance.transform);
                wall.NetworkObject.Spawn();
                wall.coords.Value = turn;

                break;
            case ETurnType.PlaceZForward:
                if (!CheckPlace(turn))
                {
                    SpesLogger.Warning("Был совершен некорректный ход при создании стенки, ход возвращен игроку" + controller.GetPlayerInfo().playerOrder);
                    controller.StartTurn();
                    return;
                }
                var wphZ = gameboard.wallsPlaces[turn.pos.x, turn.pos.y];

                var wallZ = Instantiate(wallPrefab, wphZ.transform.position, Quaternion.Euler(0, 90, 0), GameplayBase.instance.transform);
                wallZ.NetworkObject.Spawn();
                wallZ.coords.Value = turn;

                break;
            case ETurnType.DestroyXWall:
                break;
            case ETurnType.DestroyZWall:
                break;
        }

        //Если не сработал return, то ход передается следующеу игроку в следующем кадре
        activePlayer++;
        if (activePlayer >= GameBase.server.prefs.maxPlayers)
        {
            activePlayer = 0;
        }

        StartCoroutine(nextTurn());
    }

    /// <summary>
    /// Передает ход активному игроку в следующем Tick'е
    /// </summary>
    /// <returns></returns>
    private IEnumerator nextTurn()
    {
        yield return new WaitForEndOfFrame();
        yield return null;
        players[activePlayer].StartTurn();
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
        int playerOrder = players.Count;

        Vector3 playerStartPosition = playersStartPositions[playerOrder];
        float y = playerStartPosition.y;
        playerStartPosition *= GameBase.server.prefs.boardHalfExtent;
        playerStartPosition.y = y;

        var player = Instantiate(prefab.GetMono(), playerStartPosition, Quaternion.Euler(playersStartRotation[playerOrder])) as T;
        player.name = "Controller_" + playerOrder;
        players.Add(player);

        var point = PreselectedPoint(playersStartPositions[playerOrder]);

        var info = player.GetPlayerInfo();
        info.playerOrder = playerOrder;
        info.pawn = SpawnPawn(playerOrder, gameboard.blocks[point.x, point.y]);

        player.SetPlayerInfo(info);

        return player as T;
    }

    /// <summary>
    /// SERVER-FUNCTION: Создает пешку игрока, спавнит в мире, регистрирует у игрока
    /// </summary>
    /// <param name="playerOrder"></param>
    /// <param name="block"></param>
    /// <returns></returns>
    protected Pawn SpawnPawn(int playerOrder, BoardBlock block)
    {
        Pawn newPawn = Instantiate(pawnPrefab, block.transform.position, Quaternion.Euler(0, playersStartRotation[playerOrder].y, 0));
        newPawn.name = "Pawn_" + playerOrder;

        newPawn.NetworkObject.SpawnWithOwnership(ordersToNetIDs[playerOrder]);

        var info = players[playerOrder].GetPlayerInfo();
        info.pawn = newPawn;
        players[playerOrder].SetPlayerInfo(info);

        newPawn.block.Value = block.coords;
        newPawn.playerOrder = playerOrder;
        return newPawn;
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
        foreach (var pl in players)
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

    protected void S_HandleWaitingMenu()
    {
        if (IsServer)
        {
            if (players.Count == GameBase.server.prefs.maxPlayers)
            {
                ShowWaitingScreenClientRpc(false);
                players[0].StartTurn();
            }
            else
            {
                ShowWaitingScreenClientRpc(true);
            }
        }
    }

    #endregion

    #region RPCs

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
    public void GameFinishedClientRpc(int winner)
    {
        SpesLogger.Detail("Победил игрок №" + winner);

        SceneManager.LoadScene("StartupScene");
    }

    #endregion
}
