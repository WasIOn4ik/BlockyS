using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization;
using System.Threading;

public class CustomizationMenuUI : MenuBase
{
	#region Variables

	[Header("Preferences")]
	[SerializeField] private LocalizedString selectString;
	[SerializeField] private TMP_Text coinsCountText;

	[Header("Components")]
	[SerializeField] private Button backButton;

	[Header("Board skin")]
	[SerializeField] private Button boardSelectButton;
	[SerializeField] private Button boardLeftButton;
	[SerializeField] private Button boardRightButton;

	[SerializeField] private TMP_Text boardSelectText;
	[SerializeField] private Image boardFrame;

	[SerializeField] private MeshFilter boardMeshFilter;
	[SerializeField] private MeshRenderer boardMeshRenderer;

	[Header("Pawn skin")]
	[SerializeField] private Button pawnSelectButton;
	[SerializeField] private Button pawnLeftButton;
	[SerializeField] private Button pawnRightButton;

	[SerializeField] private TMP_Text pawnSelectText;
	[SerializeField] private Image pawnFrame;

	[SerializeField] private Transform pawnContainerTransform;

	private Canvas canvas;

	private SkinsLibrarySO skins;

	private int selectedBoardIndex;
	private int selectedPawnIndex;

	private BoardSkinSO boardSkin;
	private PawnSkinSO pawnSkin;

	private GameStorage storage;

	private bool bPreviousOrtho;

	#endregion

	#region UnityCallbacks

	protected override void Awake()
	{
		base.Awake();


		storage = GameBase.Storage;
		Debug.LogWarning("Awake " + storage.CurrentPawnSkinID);
		skins = GameBase.Instance.skins;

		canvas = GetComponent<Canvas>();
		canvas.worldCamera = Camera.main;

		backButton.onClick.AddListener(() =>
		{
			SoundManager.Instance.PlayBackButtonClick();
			BackToPreviousMenu();
		});

		boardLeftButton.onClick.AddListener(() =>
		{
			SoundManager.Instance.PlayButtonClick();
			skins.GetBoard(selectedBoardIndex).UnloadAll();
			selectedBoardIndex--;

			if (selectedBoardIndex < 0)
				selectedBoardIndex = skins.GetBoardSkinsCount() - 1;

			SelectBoardSkin(selectedBoardIndex);
		});

		boardRightButton.onClick.AddListener(() =>
		{
			SoundManager.Instance.PlayButtonClick();
			skins.GetBoard(selectedBoardIndex).UnloadAll();
			selectedBoardIndex++;

			if (selectedBoardIndex >= skins.GetBoardSkinsCount())
				selectedBoardIndex = 0;

			SelectBoardSkin(selectedBoardIndex);
		});

		boardSelectButton.onClick.AddListener(() =>
		{
			SoundManager.Instance.PlayButtonClick();
			if (storage.TryBuyOrEquipBoard(boardSkin))
			{
				storage.CurrentBoardSkinID = boardSkin.id;
				UpdateStats();
			}
		});

		pawnLeftButton.onClick.AddListener(() =>
		{
			SoundManager.Instance.PlayButtonClick();
			selectedPawnIndex--;

			if (selectedPawnIndex < 0)
				selectedPawnIndex = skins.GetPawnSkinsCount() - 1;

			SelectPawnSkin(selectedPawnIndex);
		});

		pawnRightButton.onClick.AddListener(() =>
		{
			SoundManager.Instance.PlayButtonClick();
			selectedPawnIndex++;

			if (selectedPawnIndex >= skins.GetPawnSkinsCount())
				selectedPawnIndex = 0;

			SelectPawnSkin(selectedPawnIndex);
		});

		pawnSelectButton.onClick.AddListener(() =>
		{
			SoundManager.Instance.PlayButtonClick();
			if (storage.TryBuyOrEquipPawn(pawnSkin))
			{
				storage.CurrentPawnSkinID = pawnSkin.id;
				UpdateStats();
			}
		});
	}

	private void OnEnable()
	{
		bPreviousOrtho = canvas.worldCamera.orthographic;
		canvas.worldCamera.orthographic = true;

		selectedBoardIndex = GameBase.Instance.skins.GetBoardIndexInList(storage.CurrentBoardSkinID);
		selectedPawnIndex = GameBase.Instance.skins.GetPawnIndexInList(storage.CurrentPawnSkinID);
		pawnSkin = GameBase.Instance.skins.GetPawn(storage.CurrentPawnSkinID);
		boardSkin = GameBase.Instance.skins.GetBoard(storage.CurrentBoardSkinID);

		Debug.LogWarning(storage.CurrentPawnSkinID);
		UpdateStats();
	}

	private void OnDisable()
	{
		if (canvas && canvas.worldCamera)
			canvas.worldCamera.orthographic = bPreviousOrtho;
		SpesLogger.Detail("Skins selected: " + storage.CurrentBoardSkinID + " " + storage.CurrentPawnSkinID);
	}

	#endregion

	#region Functions

	private void SelectBoardSkin(int skinNumber)
	{
		skins.GetBoardByListIndex(skinNumber).LoadCustomizationDisplay(x =>
		{
			boardSkin = x;

			if (x.TryGetDecorMesh(0, out var decorMesh))
			{
				boardMeshFilter.mesh = decorMesh;
			}

			if (x.TryGetMaterial(out var material))
			{
				boardMeshFilter.GetComponent<MeshRenderer>().material = material;
			}

			if (x.cost == 0 || storage.CheckBoard(x.id))
			{
				boardSelectText.text = selectString.GetLocalizedStringAsync().Result;
				boardSelectButton.interactable = storage.CurrentBoardSkinID != x.id;
			}
			else
			{
				boardSelectText.text = "<color=#FFD700>" + x.cost;
				boardSelectButton.interactable = storage.GetCoins() >= x.cost || x.cost == 0;
			}
		});
	}

	private void SelectPawnSkin(int skinNumber)
	{
		pawnSkin = skins.GetPawnByListIndex(skinNumber);
		Debug.Log($"Selecting index {skinNumber}, selected id {pawnSkin.id} {pawnSkin.title}, stored id {storage.CurrentPawnSkinID}");

		foreach (Transform ch in pawnContainerTransform)
		{
			Destroy(ch.gameObject);
		}

		pawnSkin.InstantiateTo(pawnContainerTransform, x =>
		{
			Transform tr = x.transform;
			tr.localScale = Vector3.one * pawnSkin.scale;
			tr.localRotation = Quaternion.Euler(pawnSkin.rotation);
			tr.localPosition = pawnSkin.position;

			if (pawnSkin.cost == 0 || storage.CheckPawn(pawnSkin.id))
			{
				pawnSelectText.text = selectString.GetLocalizedStringAsync().Result;
				pawnSelectButton.interactable = storage.CurrentPawnSkinID != pawnSkin.id;
			}
			else
			{
				pawnSelectText.text = "<color=#FFD700>" + pawnSkin.cost;
				pawnSelectButton.interactable = storage.GetCoins() >= pawnSkin.cost || pawnSkin.cost == 0;
			}
		});
	}

	private void UpdateStats()
	{
		SelectBoardSkin(selectedBoardIndex);
		SelectPawnSkin(selectedPawnIndex);

		coinsCountText.text = storage.GetCoins().ToString();
	}

	#endregion
}
