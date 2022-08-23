using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConnectMenu : MenuBase
{
    #region Variables

    [SerializeField] private TMPro.TMP_InputField address;
    public void OnConfrimConnectClicked()
    {
        string str = address.text;
        var add = str.Split(':');
        if (add.Length == 2)
        {
            if (ushort.TryParse(add[1], out ushort port))
            {
                GameBase.instance.ConnectToGame(add[0], port);
            }
            else
            {
                SpesLogger.Warning("��� ��������� IP:port, port �� ���������!");
            }
        }
        else
        {
            SpesLogger.Warning("��������� IP:port ����� �������� ������!");
        }
    }

    #endregion
}
