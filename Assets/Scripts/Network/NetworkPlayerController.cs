using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;

public class NetworkPlayerController : NetworkBehaviour, IPlayerController
{
    #region Variables

    protected NetworkVariable<PlayerNetworkedInfo> playerInfo = new();

    public NetworkVariable<PlayerCosmetic> cosmetic = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    [SerializeField] InGameHUD hudPrefab;

    protected InputComponent inputComp;

    protected InGameHUD hud;

    #endregion

    #region UnityCallbacks

    public void Awake()
    {
        inputComp = GetComponent<InputComponent>();
    }


    private void OnPlayerInfoChanged(PlayerNetworkedInfo previousValue, PlayerNetworkedInfo newValue)
    {
        if (!IsServer)
            hud.SetWallsCount(newValue.WallCount);
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
            var cam = Camera.main;
            cam.transform.SetParent(transform);
            cam.transform.localPosition = Vector3.zero;
            cam.transform.localRotation = Quaternion.identity;

            SpesLogger.Detail("Установлены скины: " + GameBase.storage.currentBoardSkin + " " + GameBase.storage.currentPawnSkin);
            cosmetic.Value = new PlayerCosmetic() { boardSkinID = GameBase.storage.currentBoardSkin, pawnSkinID = GameBase.storage.currentPawnSkin };
        }
    }

    #endregion

    #region IPlayerController

    /// <summary>
    /// В NetworkController'e вызывается на клиенте, а обрабатывается внутри ServerRpc
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
        SpesLogger.Deb("Локальный сетевой игрок " + GetPlayerInfo().playerOrder + " завершил ход");
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
    /// В NetworkController'e вызывается на сервере, а обрабатывается внутри RPC
    /// </summary>
    public void StartTurn()
    {
        SpesLogger.Deb("Начало хода сетевого игрока " + GetPlayerInfo().playerOrder);

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
        SpesLogger.Detail("Игрок: " + name + " скины: " + GameBase.storage.currentBoardSkin + " " + GameBase.storage.currentPawnSkin);
        return cosmetic.Value;
    }

    public void UpdateTurn(int active)
    {
        UpdateTurnClientRpc(active);
    }

    #endregion

    #region RPCs

    [ServerRpc(RequireOwnership = true, Delivery = RpcDelivery.Reliable)]
    public void EndTurnServerRpc(Turn turn)
    {
        SpesLogger.Deb("Сетевой игрок завершил ход " + GetPlayerInfo().playerOrder);

        var info = GetPlayerInfo();
        info.state = EPlayerState.Operator;
        SetPlayerInfo(info);

        GameplayBase.instance.S_EndTurn(this, turn);
    }

    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    public void StartTurnClientRpc()
    {
        SpesLogger.Deb("Локальный сетевой игрок " + GetPlayerInfo().playerOrder + " начал ход");

        if (IsOwner)
        {
            inputComp.turnValid += hud.OnTurnValidationChanged;
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
