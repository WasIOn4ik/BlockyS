using UnityEngine;

public class ConnectMenu : MenuBase
{
	#region Variables

	[SerializeField] private TMPro.TMP_InputField address;

	#endregion

	#region UIFunctions

	private void OnConfrimConnectClicked()
    {
        string str = address.text;
        var add = str.Split(':');
        if (add.Length == 2)
        {
            if (ushort.TryParse(add[1], out ushort port))
            {
                GameBase.client.ConnectToGame(add[0], port);
            }
            else
            {
                SpesLogger.Warning("Port is incorrect");
            }
        }
        else
        {
            SpesLogger.Warning("Write address:port");
        }
    }

    private void BackToMenu(string str)
    {
        GameBase.client.ClearAll();
        GoToMenu(str);
    }

    #endregion
}
