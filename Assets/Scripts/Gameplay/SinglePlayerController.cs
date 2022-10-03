using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SinglePlayerController : MonoBehaviour, IPlayerController
{
    #region Variables

    [SerializeField] protected InGameHUD hudPrefab;
    [SerializeField] protected float cameraHeight;
    [SerializeField] protected float cameraBackwardOffset;
    [SerializeField] protected float cameraMoveDuration;

    protected Vector3 cameraPosition;
    protected Quaternion cameraRotation;

    protected InputComponent inputComp;

    [SerializeField] protected PlayerInGameInfo playerInfo = new();

    [SerializeField] protected PlayerCosmetic cosmetic;

    #endregion

    #region StaticVariables

    protected static InGameHUD hud;
    protected static SinglePlayerController previousLocalController;

    #endregion

    #region UnityCallbacks

    public void Awake()
    {
        cameraPosition = transform.position;
        cameraRotation = transform.rotation;

        inputComp = GetComponent<InputComponent>();

        if (!hud)
            hud = Instantiate(hudPrefab);

        cosmetic = new PlayerCosmetic() { boardSkinID = GameBase.storage.currentBoardSkin, pawnSkinID = GameBase.storage.currentPawnSkin };
    }

    #endregion

    #region IPlayerController

    public void StartTurn()
    {
        SpesLogger.Deb("Локальный игрок " + GetPlayerInfo().playerOrder + " начал ход");

        if (previousLocalController)
            previousLocalController.GetPlayerInfo().state = EPlayerState.Waiting;

        GetPlayerInfo().state = EPlayerState.ActivePlayer;

        var cam = Camera.main;

        cam.transform.SetParent(transform);
        //transform.SetPositionAndRotation(cameraPosition, cameraRotation);
        Vector3 pos = GetPlayerInfo().pawn.transform.position + GetPlayerInfo().pawn.transform.forward * cameraBackwardOffset + Vector3.up * cameraHeight;
        transform.SetPositionAndRotation(pos, cameraRotation);

        StartCoroutine(Animate(cam));

        hud.SetInputComponent(inputComp);
        hud.ToDefault();
        inputComp.turnValid += hud.OnTurnValidationChanged;
        inputComp.UpdateTurnValid(false);
    }

    public void EndTurn(Turn turn)
    {
        SpesLogger.Deb("Локальный игрок " + GetPlayerInfo().playerOrder + " завершил ход");

        previousLocalController = this;
        GetPlayerInfo().state = EPlayerState.Operator;

        cameraPosition = transform.position;
        //cameraRotation = transform.rotation;

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

    protected IEnumerator Animate(Camera cam)
    {
        float time = Time.deltaTime;
        while (cam.transform.localPosition.magnitude > 0.005f)
        {
            cam.transform.localPosition = Vector3.Lerp(cam.transform.localPosition, Vector3.zero, time / cameraMoveDuration);
            cam.transform.localRotation = Quaternion.Lerp(cam.transform.localRotation, Quaternion.identity, time / cameraMoveDuration);

            time += Time.deltaTime;

            yield return null;
        }
    }

    public PlayerCosmetic GetCosmetic()
    {
        return cosmetic;
    }

    #endregion
}
