using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CustomizationMenu : MenuBase
{
    #region Variables
    [Header("Board skin")]
    [SerializeField] protected Button boardSelectButton;
    [SerializeField] protected Image boardFrame;
    [SerializeField] protected MeshFilter boardMesh;

    [Header("Pawn skin")]
    [SerializeField] protected Button pawnSelectButton;
    [SerializeField] protected Image pawnFrame;
    [SerializeField] protected MeshFilter pawnMesh;

    protected Canvas canvas;

    protected SkinsLibrary skins;

    protected int selectedBoard;
    protected int selectedPawn;

    private bool bPreviousOrtho;

    #endregion

    #region UnityCallbacks

    public override void Awake()
    {
        base.Awake();

        canvas = GetComponent<Canvas>();
        canvas.worldCamera = Camera.main;

        skins = GameBase.instance.skins;
    }

    #endregion

    #region Functions

    public void NextBoard()
    {
        selectedBoard++;

        if (selectedBoard >= skins.boardSkins.Count)
            selectedBoard = 0;

        SelectBoardSkin(selectedBoard);
    }

    public void PrevBoard()
    {
        selectedBoard--;

        if (selectedBoard < 0)
            selectedBoard = skins.boardSkins.Count - 1;

        SelectBoardSkin(selectedBoard);
    }

    protected void SelectBoardSkin(int skinNumber)
    {
        boardMesh.mesh = skins.boardSkins[skinNumber].variation1Prefab;
    }

    public void NextPawn()
    {
        selectedPawn++;

        if (selectedPawn >= skins.pawnSkins.Count)
            selectedPawn = 0;

        SelectPawnSkin(selectedPawn);
    }

    public void PrevPawn()
    {
        selectedPawn--;

        if (selectedPawn < 0)
            selectedPawn = skins.pawnSkins.Count - 1;

        SelectPawnSkin(selectedPawn);
    }

    protected void SelectPawnSkin(int skinNumber)
    {
        pawnMesh.mesh = skins.pawnSkins[skinNumber].mesh;
    }

    public void OnEnable()
    {
        bPreviousOrtho = canvas.worldCamera.orthographic;
        canvas.worldCamera.orthographic = true;
    }

    public void OnDisable()
    {
        canvas.worldCamera.orthographic = bPreviousOrtho;
    }

    #endregion
}
