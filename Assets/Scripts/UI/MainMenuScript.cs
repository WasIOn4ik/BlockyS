using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenuScript : MenuBase
{
    #region Variables

    [SerializeField] private GameObject connectMenu, hostMenu, settingsMenu;

    #endregion

    #region UnityCallbacks



    #endregion

    #region UIFunctions

    public void OnConnectClicked()
    {
        Destroy(gameObject);
    }

    #endregion
}
