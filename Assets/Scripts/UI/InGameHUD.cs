using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

    [Header("Preferences")]
    [Tooltip("»конка, котора€ показываетс€, когда действие по нажатию кнопки - движение пешки")]
    [SerializeField] protected Sprite moveTurnSprite;

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
        SpesLogger.Detail(b.ToString());
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

    #endregion
}
