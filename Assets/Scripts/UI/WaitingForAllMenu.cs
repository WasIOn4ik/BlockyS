
using TMPro;
using UnityEngine;
using UnityEngine.Localization;

public enum WaitingState
{
	Loading,
	WaitingOtherPlayers
}

public class WaitingForAllMenu : MenuBase
{
	#region Variables

	[Header("Peoperties")]
	[SerializeField] private float rotationSpeed;

	[Header("Components")]
	[SerializeField] private RectTransform loadingRect;
	[SerializeField] private TMP_Text messageText;
	[SerializeField] private LocalizedString loadingMessage;
	[SerializeField] private LocalizedString waitingMessage;

	private WaitingState currentState;

	#endregion

	#region UnityCallbacks

	private void Update()
	{
		loadingRect.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
	}

	#endregion

	#region Functions

	public void SetState(WaitingState state)
	{
		var message = state == WaitingState.Loading ? loadingMessage : waitingMessage;

		message.GetLocalizedStringAsync().Completed += s =>
		{
			if (s.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
			{
				messageText.text = s.Result;
			}
		};
	}

	#endregion
}
