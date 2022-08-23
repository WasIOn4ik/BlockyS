using System.Collections;
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

    #region UIFunctions

    public void OnLanguageChanged()
    {
        var settings = LocalizationSettings.Instance;
        string code = locales[languageDD.value];
        var locale = settings.GetAvailableLocales().GetLocale(code);
        settings.SetSelectedLocale(locale);
    }

    public void OnSaveClicked()
    {

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
