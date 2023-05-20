using UnityEngine;
using UnityEngine.UI;

public class CreditsMenuUI : MenuBase
{
	#region Variables

	[SerializeField] private Button backButton;

	#endregion

	#region UnityCallbacks

	protected override void Awake()
	{
		base.Awake();

		backButton.onClick.AddListener(() =>
		{
			BackToPreviousMenu();
		});
	}

	#endregion

}
