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
    /// При обновлении значения сохраняет изменения
    /// </summary>
    public int CurrentBoardSkin { get { return prefs.selectedBoardSkin; } set { prefs.selectedBoardSkin = value; SavePrefs(); } }

    /// <summary>
    /// При обновлении значения сохраняет изменения
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
            SpesLogger.Detail("Загрузка настроек прошла успешно");
            GameBase.client.playerName = prefs.playerName;
            return true;
        }
        SavePrefs();
        SpesLogger.Detail("Настройки были сохранены в первый раз");
        return false;
    }

    public void SavePrefs()
    {
        string jsonString = JsonUtility.ToJson(prefs);
        PlayerPrefs.SetString(preferencesString, jsonString);
        PlayerPrefs.Save();
        SpesLogger.Detail("Сохранение настроек прошло успешно");
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

            SpesLogger.Detail("Загрузка игровых данных прошла успешно");

            return true;
        }
        catch (Exception ex)
        {
            SpesLogger.Exception(ex, "Ошибка загрузки игровых данных");
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

        SpesLogger.Detail("Сохранение игровых данных прошло успешно");
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
            SpesLogger.Detail("Доска " + skin.name + " выбрана");
            return true;
        }

        if (GetCoins() >= skin.cost)
        {
            progress.coins -= skin.cost;
            progress.availableBoardSkins.Add(id);

            CurrentBoardSkin = id;

            SaveProgress();

            SpesLogger.Detail("Доска " + skin.name + " Куплена и выбрана");

            return true;
        }

        SpesLogger.Detail("Недостаточно средст для покупки " + skin.name + " " + GetCoins() + "/" + skin.cost);
        return false;
    }

    public bool TryBuyOrEquipPawn(int id)
    {
        var skin = GameBase.instance.skins.pawnSkins[id];

        if (CheckPawn(id) || skin.cost == 0)
        {
            SpesLogger.Detail("Пешка " + skin.name + " выбрана");
            return true;
        }

        if (GetCoins() >= skin.cost)
        {
            progress.coins -= skin.cost;
            progress.availablePawnSkins.Add(id);

            CurrentPawnSkin = id;

            SaveProgress();

            SpesLogger.Detail("Пешка " + skin.name + " куплена и выбрана");

            return true;
        }

        SpesLogger.Detail("Недостаточно средст для покупки " + skin.name + " " + GetCoins() + "/" + skin.cost);
        return false;
    }

    #endregion
}

