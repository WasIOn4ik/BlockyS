using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SinglePlayerController : MonoBehaviour, IPlayerController
{
    #region Variables

    [SerializeField] protected InGameHUD hudPrefab;
    [SerializeField] protected float cameraHeight;
    [SerializeField] protected float cameraBackwardOffset;

    protected Vector3 cameraPosition;
    protected Quaternion cameraRotation;

    protected InputComponent inputComp;

    [SerializeField] protected PlayerInGameInfo playerInfo = new();

    [SerializeField] protected PlayerCosmetic cosmetic;

    #endregion

    #region StaticVariables

    protected static InGameHUD hud;
    protected static SinglePlayerController previousLocalController;

    protected static bool bTurnDisplayUpToDate = false;

    #endregion

    #region UnityCallbacks

    public void Awake()
    {
        cameraPosition = transform.position;
        cameraRotation = transform.rotation;

        inputComp = GetComponent<InputComponent>();

        if (!hud)
            hud = Instantiate(hudPrefab);

        cosmetic = new PlayerCosmetic() { boardSkinID = GameBase.storage.CurrentBoardSkin, pawnSkinID = GameBase.storage.CurrentPawnSkin };
    }

    #endregion

    #region IPlayerController

    public void StartTurn()
    {
        SpesLogger.Deb("Локальный игрок " + GetPlayerInfo().playerOrder + " начал ход");

        if (previousLocalController)
        {
            var linfo = previousLocalController.GetPlayerInfo();
            linfo.state = EPlayerState.Operator;
            previousLocalController.SetPlayerInfo(linfo);
        }

        var info = GetPlayerInfo();
        info.state = EPlayerState.ActivePlayer;
        SetPlayerInfo(info);

        //Локальные переменные
        var cam = Camera.main;
        var tp = cam.transform.position;
        var tr = cam.transform.rotation;

        cam.transform.SetParent(transform);

        //Расчет нового положения камеры
        Vector3 pos = GetPlayerInfo().pawn.transform.position + GetPlayerInfo().pawn.transform.forward * cameraBackwardOffset + Vector3.up * cameraHeight;
        transform.SetPositionAndRotation(pos, cameraRotation);

        //Настройка компонентов
        hud.SetInputComponent(inputComp);
        hud.ToDefault();
        hud.SetWallsCount(GetPlayerInfo().WallCount);
        inputComp.turnValid += hud.OnTurnValidationChanged;
        inputComp.UpdateTurnValid(false);

        //Инициализация камеры при старте игры или компенсация "дрожания камеры" при передаче хода
        if (GameplayBase.instance.bGameActive)
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
        SpesLogger.Deb("Локальный игрок " + GetPlayerInfo().playerOrder + " завершил ход");

        previousLocalController = this;

        var info = GetPlayerInfo();
        info.state = EPlayerState.Operator;
        SetPlayerInfo(info);

        cameraPosition = transform.position;

        GameplayBase.instance.S_EndTurn(this, turn);

        inputComp.turnValid -= hud.OnTurnValidationChanged;
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
        if (bTurnDisplayUpToDate)
            return;

        bTurnDisplayUpToDate = true;
        StartCoroutine(UpdateDisplayTurn(active));
    }

    public IEnumerator UpdateDisplayTurn(int active)
    {
        yield return null;

        hud.SetPlayerTurn(active);

        bTurnDisplayUpToDate = false;
    }

    #endregion
}

