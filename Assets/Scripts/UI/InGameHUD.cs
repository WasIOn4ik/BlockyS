using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using TMPro;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.SmartFormat.PersistentVariables;

[RequireComponent(typeof(Animator))]
public class InGameHUD : MonoBehaviour
{
    #region Variables

    [Header("Components")]
    [Tooltip("»конка, котора€ показываетс€, когда действие по нажатию кнопки - создание стены")]
    [SerializeField] protected Image placeWallButtonImage;

    [Tooltip(" нопка подтверждени€ хода")]
    [SerializeField] protected Button confirmTurnButton;

    [Tooltip("»конка на кнопке подтверждени€ хода")]
    [SerializeField] protected Image confirmTurnImage;

    [SerializeField] protected LocalizeStringEvent turnHelperLocalizeEvent;
    [SerializeField] protected TMP_Text wallsCountText;

    [Header("Preferences")]
    [Tooltip("»конка, котора€ показываетс€, когда действие по нажатию кнопки - движение пешки")]
    [SerializeField] protected Sprite moveTurnSprite;
    [SerializeField] protected LocalizedString yourTurnLocalized;
    [SerializeField] protected LocalizedString oponentTurnLocalized;

    protected InputComponent inputComp;

    protected Sprite buildWallSprite;

    protected Animator animator;

    #endregion

    #region UnityCallbacks

    public void Awake()
    {
        buildWallSprite = placeWallButtonImage.sprite;
        animator = GetComponent<Animator>();
    }

    #endregion

    #region UIFUnctions

    public void OnPlaceWallClicked()
    {
        inputComp.SetMoveMode(!inputComp.GetMoveMode());

        placeWallButtonImage.sprite = inputComp.GetMoveMode() ? buildWallSprite : moveTurnSprite;

        animator.Play(inputComp.GetMoveMode() ? "HideConfirmTurn" : "ShowConfirmTurn");
    }

    public void OnConfirmTurnClicked()
    {
        inputComp.ConfirmTurn();
        animator.Play("HideConfirmTurn");
    }

    public void OnDestroyWallClicked()
    {

    }

    #endregion

    #region Functions

    public void OnTurnValidationChanged(bool b)
    {
        confirmTurnButton.interactable = b;
    }

    public void SetInputComponent(InputComponent comp)
    {
        inputComp = comp;
    }

    public void ToDefault()
    {
        if (!inputComp.GetMoveMode())
        {
            inputComp.SetMoveMode(true);

            placeWallButtonImage.sprite = buildWallSprite;

            animator.Play("HideConfirmTurn");
        }
    }

    public void SetPlayerTurn(int activePlayer)
    {
        bool local = inputComp.controller.GetPlayerInfo().playerOrder == activePlayer;
        SpesLogger.Detail("ќбновлено отображение хода ");

        if (local)
        {
            var iv = yourTurnLocalized["playerNum"] as IntVariable;
            iv.Value = activePlayer;
            turnHelperLocalizeEvent.StringReference = yourTurnLocalized;
        }
        else
        {
            var iv = oponentTurnLocalized["playerNum"] as IntVariable;
            iv.Value = activePlayer;
            turnHelperLocalizeEvent.StringReference = oponentTurnLocalized;
        }
        turnHelperLocalizeEvent.RefreshString();
    }

    public void SetWallsCount(int x)
    {
        wallsCountText.text = " X " + x;
    }

    #endregion
}
