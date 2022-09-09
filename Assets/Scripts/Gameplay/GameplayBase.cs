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
    [Tooltip("X � Z ���������� �� halfExtent, � Y �������� ��� ���������")]
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
        SpesLogger.Detail("GmplB: ������ " + clientID + " �����������");
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

        SpesLogger.Detail("GmplB: networkSpawn " + (IsServer ? "{������}" : "{������}"));

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
    /// SERVER-FUNCTION: ���������� �������� � ��������� ����� GameplayBase ��� ���������� ����
    /// </summary>
    /// <param name="controller"></param>
    /// <param name="turn"></param>
    public void S_EndTurn(IPlayerController controller, Turn turn)
    {
        switch (turn.type)
        {
            case ETurnType.Move:
                var pawn = players[activePlayer].GetPlayerInfo().pawn;

                if (!CheckMove(pawn, turn))
                {
                    controller.StartTurn();
                    return;
                }

                var block = gameboard.blocks[turn.pos.x, turn.pos.y];

                pawn.transform.position = block.transform.position;

                pawn.block.Value = block.coords;

                if (pawn.block.Value.x == GameBase.server.prefs.boardHalfExtent && pawn.block.Value.y == GameBase.server.prefs.boardHalfExtent)
                {
                    GameFinishedClientRpc(controller.GetPlayerInfo().playerOrder);
                }
                break;

            case ETurnType.PlaceXForward:
                var wph = gameboard.wallsPlaces[turn.pos.x, turn.pos.y];

                Instantiate(wallPrefab, wph.transform.position, Quaternion.Euler(0, 90, 0), GameplayBase.instance.transform);

                break;
            case ETurnType.PlaceZForward:
                var wphZ = gameboard.wallsPlaces[turn.pos.x, turn.pos.y];

                Instantiate(wallPrefab, wphZ.transform.position, Quaternion.identity, GameplayBase.instance.transform);
                break;
            case ETurnType.DestroyXWall:
                break;
            case ETurnType.DestroyZWall:
                break;
        }

        //���� �� �������� return, �� ��� ���������� ��������� ������ � ��������� �����
        activePlayer++;
        if (activePlayer >= GameBase.server.prefs.maxPlayers)
        {
            activePlayer = 0;
        }

        StartCoroutine(nextTurn());
    }

    /// <summary>
    /// �������� ��� ��������� ������ � ��������� Tick'�
    /// </summary>
    /// <returns></returns>
    private IEnumerator nextTurn()
    {
        yield return new WaitForEndOfFrame();
        yield return null;
        players[activePlayer].StartTurn();
    }

    /// <summary>
    /// ����������� ���������� ��������� ������� Vector3 �� x,y,z[0:1] � Point x,y[0:halfExt*2]
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
    /// SERVER-FUNCTION: ������� ���������� ������.
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
    /// SERVER-FUNCTION: ������� ����� ������, ������� � ����, ������������ � ������
    /// </summary>
    /// <param name="playerOrder"></param>
    /// <param name="block"></param>
    /// <returns></returns>
    protected Pawn SpawnPawn(int playerOrder, BoardBlock block)
    {
        Pawn newPawn = Instantiate(pawnPrefab, block.transform.position, Quaternion.identity);
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
    /// ��������� ����������� ������������ ����� � ������������ �������
    /// </summary>
    /// <param name="pawn"></param>
    /// <param name="turn"></param>
    /// <returns></returns>
    protected bool CheckMove(Pawn pawn, Turn turn)
    {
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
        SpesLogger.Detail("������� ����� �" + winner);

        SceneManager.LoadScene("StartupScene");
    }

    #endregion
}
