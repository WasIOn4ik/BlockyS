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
        SelectBoardSkin(GameBase.storage.currentBoardSkin);
        SelectPawnSkin(GameBase.storage.currentPawnSkin);
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
        var skin = skins.boardSkins[skinNumber];
        boardMesh.mesh = skin.variation1Mesh;
        boardMesh.GetComponent<MeshRenderer>().material = skin.mat;
        GameBase.storage.currentBoardSkin = skinNumber;
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
        var skin = skins.pawnSkins[skinNumber];
        pawnMesh.mesh = skin.mesh;
        pawnMesh.GetComponent<MeshRenderer>().material = skin.mat;
        pawnMesh.transform.localScale = Vector3.one * skin.scale;
        pawnMesh.transform.localRotation = Quaternion.Euler(skin.rotation);
        pawnMesh.transform.localPosition = skin.position;
        GameBase.storage.currentPawnSkin = skinNumber;
    }

    public void OnEnable()
    {
        bPreviousOrtho = canvas.worldCamera.orthographic;
        canvas.worldCamera.orthographic = true;
    }

    public void OnDisable()
    {
        canvas.worldCamera.orthographic = bPreviousOrtho;
        GameBase.storage.currentPawnSkin = selectedPawn;
        GameBase.storage.currentBoardSkin = selectedBoard;
        SpesLogger.Detail("Выбраны скины: " + selectedBoard + " " + selectedPawn);
    }

    #endregion
}
