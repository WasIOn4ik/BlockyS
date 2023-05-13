using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;

public class SettingsMenu : MenuBase
{
    #region Variables

    [SerializeField] private TMP_Dropdown languageDD;
    [SerializeField] private TMP_InputField playerNameInput;

    [SerializeField] private List<string> locales = new List<string>();

    #endregion

    #region UnityCallbacks

    public override void Awake()
    {
        base.Awake();
        GameStorage storage = GameBase.storage;
        int languageIndex = 0;
        languageDD.ClearOptions();
        locales.Clear();
        List<string> strings = new();
        foreach (var loc in LocalizationSettings.Instance.GetAvailableLocales().Locales)
        {
            strings.Add(loc.LocaleName);
            locales.Add(loc.Identifier.Code);
        }
        languageDD.AddOptions(strings);

        playerNameInput.text = storage.prefs.playerName;

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

    #region UIFunctions

    public void OnLanguageChanged()
    {
        string code = locales[languageDD.value];
        GameBase.instance.ApplyLanguage(code);
    }

    public void OnSaveClicked()
    {
        var storage = GameBase.storage;
        storage.prefs.playerName = playerNameInput.text;
        storage.SavePrefs();
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
