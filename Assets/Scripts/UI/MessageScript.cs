using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using TMPro;
using UnityEngine.SceneManagement;

public enum MessageAction
{
    Close,
    LoadScene,
    OpenMenu
}

/// <summary>
/// После создания необходимо вызвать ShowMessage Для показа сообщения
/// </summary>
public class MessageScript : MonoBehaviour
{
    #region Variables

    [SerializeField] protected TMP_Text message;

    protected string actionParam;

    protected MessageAction buttonAction;

    #endregion

    #region Fuctions

    /// <summary>
    /// Заполнение содержимого
    /// </summary>
    /// <param name="entry">Вхождение для таблицы локализации Messages или текст для показа</param>
    /// <param name="action">Тип совершаемого действия при нажатии на кнопку</param>
    /// <param name="bLocalized">Использовать таблицу локализации(true) или чистый текст(false)</param>
    /// <param name="param">Параметры совершаемого действия, Не влияет на Close</param>
    public void ShowMessage(string entry, MessageAction action, bool bLocalized = true, string param = "")
    {
        gameObject.SetActive(true);
        if (bLocalized)
        {
            LocalizedString str = new LocalizedString("Messages", entry);
            message.text = str.GetLocalizedString();
        }
        else
        {
            message.text = entry;
        }
        actionParam = param;
        buttonAction = action;
    }

    public void OnButtonPressed()
    {
        switch (buttonAction)
        {
            case MessageAction.Close:
                gameObject.SetActive(false);
                break;
            case MessageAction.LoadScene:
                SceneManager.LoadScene(actionParam);
                break;
            case MessageAction.OpenMenu:
                MenuBase.OpenMenu(actionParam);
                break;
        }
    }

    #endregion
}
