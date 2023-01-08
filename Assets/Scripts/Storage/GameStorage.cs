using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

[Serializable]
public struct Preferences
{
    public string playerName;

    public float masterVolume;
    public float musicVolume;

    public int selectedBoardSkin;
    public int selectedPawnSkin;
}

[Serializable]
public struct Progress
{
    public List<int> availableBoardSkins;
    public List<int> availablePawnSkins;
    public int coins;
}

public class GameStorage : MonoBehaviour
{
    #region Variables

    [SerializeField] protected string preferencesString;

    [SerializeField] protected string progressString;

    public Preferences prefs = new() { masterVolume = 1.0f, musicVolume = 1.0f, selectedBoardSkin = 0, selectedPawnSkin = 0, playerName = "Guest" };

    public Progress progress = new() { availableBoardSkins = new(), availablePawnSkins = new(), coins = 0 };

    /// <summary>
    /// ��� ���������� �������� ��������� ���������
    /// </summary>
    public int CurrentBoardSkin { get { return prefs.selectedBoardSkin; } set { prefs.selectedBoardSkin = value; SavePrefs(); } }

    /// <summary>
    /// ��� ���������� �������� ��������� ���������
    /// </summary>
    public int CurrentPawnSkin { get { return prefs.selectedPawnSkin; } set { prefs.selectedPawnSkin = value; SavePrefs(); } }

    #endregion

    #region Functions

    public bool LoadPrefs()
    {
        if (PlayerPrefs.HasKey(preferencesString))
        {
            string jsonString = PlayerPrefs.GetString(preferencesString);
            prefs = JsonUtility.FromJson<Preferences>(jsonString);
            SpesLogger.Detail("�������� �������� ������ �������");
            GameBase.client.playerName = prefs.playerName;
            return true;
        }
        SavePrefs();
        SpesLogger.Detail("��������� ���� ��������� � ������ ���");
        return false;
    }

    public void SavePrefs()
    {
        string jsonString = JsonUtility.ToJson(prefs);
        PlayerPrefs.SetString(preferencesString, jsonString);
        PlayerPrefs.Save();
        SpesLogger.Detail("���������� �������� ������ �������");
        GameBase.client.playerName = prefs.playerName;
    }

    public bool LoadProgress()
    {
        try
        {
            BinaryFormatter bf = new BinaryFormatter();
            string configFolder = Application.persistentDataPath + "/Config/";
            string path = configFolder + progressString + ".spd";

            FileStream file;

            if (!Directory.Exists(configFolder))
            {
                Directory.CreateDirectory(configFolder);
            }

            if (!File.Exists(path))
            {
                SaveProgress();
            }

            file = File.OpenRead(path);

            progress = (Progress)bf.Deserialize(file);
            file.Close();

            SpesLogger.Detail("�������� ������� ������ ������ �������");

            return true;
        }
        catch (Exception ex)
        {
            SpesLogger.Exception(ex, "������ �������� ������� ������");
            return false;
        }
    }

    public void SaveProgress()
    {
        BinaryFormatter bf = new BinaryFormatter();
        string configFolder = Application.persistentDataPath + "/Config/";
        string path = configFolder + progressString + ".spd";
        FileStream file;

        if (!Directory.Exists(configFolder))
        {
            Directory.CreateDirectory(configFolder);
        }

        if (File.Exists(path))
        {
            file = File.OpenWrite(path);
        }
        else
        {
            file = File.Create(path);
        }
        bf.Serialize(file, progress);
        file.Close();

        SpesLogger.Detail("���������� ������� ������ ������ �������");
    }

    public bool CheckBoard(int id)
    {
        return progress.availableBoardSkins.Contains(id);
    }

    public bool CheckPawn(int id)
    {
        return progress.availablePawnSkins.Contains(id);
    }

    public int GetCoins()
    {
        return progress.coins;
    }

    public bool TryBuyOrEquipBoard(int id)
    {
        var skin = GameBase.instance.skins.boardSkins[id];

        if (CheckBoard(id) || skin.cost == 0)
        {
            SpesLogger.Detail("����� " + skin.name + " �������");
            return true;
        }

        if (GetCoins() >= skin.cost)
        {
            progress.coins -= skin.cost;
            progress.availableBoardSkins.Add(id);

            CurrentBoardSkin = id;

            SaveProgress();

            SpesLogger.Detail("����� " + skin.name + " ������� � �������");

            return true;
        }

        SpesLogger.Detail("������������ ������ ��� ������� " + skin.name + " " + GetCoins() + "/" + skin.cost);
        return false;
    }

    public bool TryBuyOrEquipPawn(int id)
    {
        var skin = GameBase.instance.skins.pawnSkins[id];

        if (CheckPawn(id) || skin.cost == 0)
        {
            SpesLogger.Detail("����� " + skin.name + " �������");
            return true;
        }

        if (GetCoins() >= skin.cost)
        {
            progress.coins -= skin.cost;
            progress.availablePawnSkins.Add(id);

            CurrentPawnSkin = id;

            SaveProgress();

            SpesLogger.Detail("����� " + skin.name + " ������� � �������");

            return true;
        }

        SpesLogger.Detail("������������ ������ ��� ������� " + skin.name + " " + GetCoins() + "/" + skin.cost);
        return false;
    }

    #endregion
}

