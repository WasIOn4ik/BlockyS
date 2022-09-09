using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Animator))]
public class InGameHUD : MonoBehaviour
{
    [SerializeField] protected Image placeWallButtonImage;

    [SerializeField] protected Sprite cancelSprite;

    public InputComponent inputComp;

    protected Sprite buildWallSprite;

    protected Animator animator;

    public void Awake()
    {
        buildWallSprite = placeWallButtonImage.sprite;
        animator = GetComponent<Animator>();
    }
    public void OnPlaceWallClicked()
    {
        inputComp.bMoveMode = !inputComp.bMoveMode;

        placeWallButtonImage.sprite = inputComp.bMoveMode ? buildWallSprite : cancelSprite;

        animator.Play(inputComp.bMoveMode ? "HideConfirm" : "ShowConfirm");
    }

    public void OnConfirmTurnClicked()
    {
        inputComp.ConfirmTurn();
        animator.Play("HideConfirm");
    }

    public void OnDestroyWallClicked()
    {

    }
}
