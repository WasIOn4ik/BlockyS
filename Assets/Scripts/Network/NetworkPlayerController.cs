using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;

public class NetworkPlayerController : NetworkBehaviour, IPlayerController
{
    #region Variables

    public NetworkVariable<PlayerCosmetic> cosmetic = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    [SerializeField] private InGameHUD hudPrefab;

    protected NetworkVariable<PlayerNetworkedInfo> playerInfo = new();

    protected InputComponent inputComp;

    protected InGameHUD hud;

    protected Camera cam;


    #endregion

    #region UnityCallbacks

    public void Awake()
    {
        inputComp = GetComponent<InputComponent>();
    }

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
            cam.transform.SetParent(transform);
            cam.transform.localPosition = Vector3.zero;
            cam.transform.localRotation = Quaternion.identity;
            AllignCamera();

            SpesLogger.Detail("����������� �����: " + GameBase.storage.CurrentBoardSkin + " " + GameBase.storage.CurrentPawnSkin);
            cosmetic.Value = new PlayerCosmetic() { boardSkinID = GameBase.storage.CurrentBoardSkin, pawnSkinID = GameBase.storage.CurrentPawnSkin };
        }
    }

    private void OnPlayerInfoChanged(PlayerNetworkedInfo previousValue, PlayerNetworkedInfo newValue)
    {
        if (!IsServer)
            hud.SetWallsCount(newValue.WallCount);
    }

    #endregion

    #region IPlayerController

    /// <summary>
    /// � NetworkController'e ���������� �� �������, � �������������� ������ ServerRpc
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
        SpesLogger.Deb("��������� ������� ����� " + GetPlayerInfo().playerOrder + " �������� ���");

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
    /// � NetworkController'e ���������� �� �������, � �������������� ������ RPC
    /// </summary>
    public void StartTurn()
    {
        SpesLogger.Deb("������ ���� �������� ������ " + GetPlayerInfo().playerOrder);

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
        SpesLogger.Detail("�����: " + name + " �����: " + GameBase.storage.CurrentBoardSkin + " " + GameBase.storage.CurrentPawnSkin);
        return cosmetic.Value;
    }

    public void UpdateTurn(int active)
    {
        UpdateTurnClientRpc(active);
    }

    /// <summary>
    /// ����������� ���������� �� ������ ���������
    /// </summary>
    /// <returns>������� ��������� ������ �� ����������</returns>
    protected Vector3 AllignCamera()
    {
        var cameraPosition = cam.transform.position;
        var cameraRotation = cam.transform.rotation;
        //������ ������ ��������� ������
        Vector3 pos = GetPlayerInfo().pawn.transform.position + GetPlayerInfo().pawn.transform.forward * GameBase.instance.gameRules.cameraBackwardOffset + Vector3.up * GameBase.instance.gameRules.cameraHeight;
        transform.SetPositionAndRotation(pos, cameraRotation);
        return cameraPosition;
    }

    #endregion

    #region RPCs

    [ServerRpc(RequireOwnership = true, Delivery = RpcDelivery.Reliable)]
    public void EndTurnServerRpc(Turn turn)
    {
        SpesLogger.Deb("������� ����� �������� ��� " + GetPlayerInfo().playerOrder);

        var info = GetPlayerInfo();
        info.state = EPlayerState.Operator;
        SetPlayerInfo(info);

        GameplayBase.instance.S_EndTurn(this, turn);
    }

    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    public void StartTurnClientRpc()
    {
        SpesLogger.Deb("��������� ������� ����� " + GetPlayerInfo().playerOrder + " ����� ���");

        if (IsOwner)
        {
            inputComp.turnValid += hud.OnTurnValidationChanged;

            cam.transform.position = AllignCamera();
        }

        inputComp.UpdateTurnValid(false);
    }

    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    public void UpdateTurnClientRpc(int active)
    {
        if (!IsServer)
            hud.SetPlayerTurn(active);
    }

    #endregion
}
