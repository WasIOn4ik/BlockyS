using UnityEngine;

public class MainMenuUI : MenuBase
{
    #region Variables

    [SerializeField] private GameObject connectMenu, hostMenu, settingsMenu;

    #endregion

    #region UnityCallbacks



    #endregion

    #region UIFunctions

    private void OnConnectClicked()
    {
        Destroy(gameObject);
    }

    #endregion
}
