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
	[SerializeField] private Image placeWallButtonImage;

	[SerializeField] private Button confirmTurnButton;
	[SerializeField] private Button placeMoveButton;

	[SerializeField] private Image confirmTurnImage;

	[SerializeField] private LocalizeStringEvent turnHelperLocalizeEvent;
	[SerializeField] private TMP_Text wallsCountText;

	[Header("Preferences")]
	[SerializeField] private Sprite moveTurnSprite;
	[SerializeField] private Sprite buildWallSprite;
	[SerializeField] private LocalizedString yourTurnLocalized;
	[SerializeField] private LocalizedString oponentTurnLocalized;
	[SerializeField] private Image timeImage;

	private InputComponent inputComp;

	private Animator animator;

	private float turnStartTime;

	private float turnTime;

	#endregion

	#region UnityCallbacks

	private void Awake()
	{
		buildWallSprite = placeWallButtonImage.sprite;
		animator = GetComponent<Animator>();
		turnTime = GameBase.Instance.gameRules.turnTime;

		confirmTurnButton.onClick.AddListener(() =>
		{
			inputComp.ConfirmTurn();
			animator.Play("HideConfirmTurn");
		});

		placeMoveButton.onClick.AddListener(() =>
		{
			OnPlaceWallClicked();
		});
	}

	#endregion

	#region UIFUnctions

	private void OnPlaceWallClicked()
	{
		if (inputComp.GetMoveMode())
		{
			if (inputComp.controller.GetPlayerInfo().WallCount <= 0)
			{
				animator.Play("WallsCountWarning");
				return;
			}
		}

		inputComp.SetMoveMode(!inputComp.GetMoveMode());

		UpdateActionButton();

		animator.Play(inputComp.GetMoveMode() ? "HideConfirmTurn" : "ShowConfirmTurn");
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
		UpdateActionButton();


		if (!inputComp.GetMoveMode())
		{
			inputComp.SetMoveMode(true);

			animator.Play("HideConfirmTurn");
		}
	}

	public void SetPlayerTurn(int activePlayer)
	{
		bool local = inputComp.controller.GetPlayerInfo().playerOrder == activePlayer;
		SpesLogger.Detail("Turn display updated");

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

		turnStartTime = Time.time;
		StartCoroutine(updateFillAmount());
		turnHelperLocalizeEvent.RefreshString();
	}

	public void SetWallsCount(int x)
	{
		wallsCountText.text = " X " + x;
	}

	private void UpdateActionButton()
	{
		placeWallButtonImage.sprite = inputComp.GetMoveMode() ? buildWallSprite : moveTurnSprite;
	}

	private IEnumerator updateFillAmount()
	{
		float remain = (turnTime + turnStartTime - Time.time);
		while (remain > 0.5f)
		{
			remain = (turnTime + turnStartTime - Time.time);
			timeImage.fillAmount = remain / turnTime;

			yield return new WaitForSeconds(1);
		}
		StopCoroutine(updateFillAmount());
	}
	#endregion
}
