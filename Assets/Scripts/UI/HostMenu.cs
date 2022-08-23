using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HostMenu : MenuBase
{
    #region Variables

    [SerializeField] private TMP_InputField portInput;
    [SerializeField] private Slider playersCountSlider;
    [SerializeField] private TMP_Text playersCountText;

    #endregion

    #region UIFunctions

    public void OnPlayersCountChanged()
    {
        playersCountText.text = playersCountSlider.value.ToString() + " / 4";
    }

    public void OnHostClicked()
    {
        GameBase.instance.HostGame(2545);
    }

    #endregion
}
