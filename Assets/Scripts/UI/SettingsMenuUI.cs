using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class SettingsMenuUI : MenuBase
{
	#region Variables

	[SerializeField] private TMP_Dropdown languageDD;
	[SerializeField] private TMP_InputField playerNameInput;
	[SerializeField] private Button backButton;
	[SerializeField] private Button confirmButton;

	[SerializeField] private List<string> locales = new List<string>();

	private GameStorage storage;

	#endregion

	#region UnityCallbacks

	protected override void Awake()
	{
		base.Awake();

		List<string> strings = new();

		storage = GameBase.Storage;

		int languageIndex = 0;
		languageDD.ClearOptions();
		locales.Clear();

		foreach (var loc in LocalizationSettings.Instance.GetAvailableLocales().Locales)
		{
			strings.Add(loc.LocaleName);
			locales.Add(loc.Identifier.Code);
		}
		languageDD.AddOptions(strings);

		languageDD.onValueChanged.AddListener((x) =>
		{
			string code = locales[languageDD.value];
			GameBase.Instance.ApplyLanguage(code);
		});

		confirmButton.onClick.AddListener(() =>
		{
			storage.PlayerName = playerNameInput.text;
			storage.SavePrefs();
		});

		backButton.onClick.AddListener(() =>
		{
			BackToPreviousMenu();
		});

		playerNameInput.text = storage.PlayerName;

		for (int i = 0; i < locales.Count; i++)
		{
			if (locales[i] == LocalizationSettings.Instance.GetSelectedLocale().Identifier.Code)
			{
				languageIndex = i;
				break;
			}
		}

		languageDD.value = languageIndex;
	}

	#endregion

	#region Functions

	public override void Reset()
	{
		base.Reset();

		playerNameInput.text = "";
		var settings = LocalizationSettings.Instance;
		int langID = locales.FindIndex(x => { return x == settings.GetSelectedLocale().Identifier.Code; });
		languageDD.value = langID;
	}


	#endregion
}
