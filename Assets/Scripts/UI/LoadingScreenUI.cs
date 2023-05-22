
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;


public class LoadingScreenUI : MonoBehaviour
{
	#region Variables


	[Header("Peoperties")]
	[SerializeField] private float rotationSpeed;

	[Header("Components")]
	[SerializeField] private RectTransform loadingRect;

	private float timeToWait;
	private bool bCloseOnTimeout;

	private Action onHideAction;

	#endregion

	#region UnityCallbacks

	private void Awake()
	{
		DontDestroyOnLoad(gameObject);
	}

	private void Update()
	{
		loadingRect.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);

		timeToWait -= Time.deltaTime;

		if (timeToWait <= 0 && bCloseOnTimeout)
			Hide();
	}

	#endregion

	#region Functions

	public void Setup(float time = 0, Action onHide = null)
	{
		timeToWait = time;
		bCloseOnTimeout = false;
		gameObject.SetActive(true);
		onHideAction = onHide;
	}

	#endregion

	#region Overrides

	public void Hide()
	{
		if (timeToWait <= 0)
		{
			gameObject.SetActive(false);
			onHideAction?.Invoke();
		}
		else
			bCloseOnTimeout = true;
	}

	#endregion
}
