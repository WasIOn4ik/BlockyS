using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class MenuBase : MonoBehaviour
{
    #region Variables

    [SerializeField] public string title;

    #endregion

    #region StaticVariables

    protected static List<MenuBase> loadedMenus = new List<MenuBase>();

    protected static MenusLibrary menus;

    #endregion

    #region StaticFunctions

    protected static bool FindInLoadedMenus(string title)
    {
        foreach (var m in loadedMenus)
        {
            if (m)
                if (m.title == title)
                    return true;
        }

        return false;
    }

    protected static MenuBase GetFromLoaded(string title)
    {
        //return loadedMenus.Find(x => { return x.title == title; });
        MenuBase m = null;
        for (int i = 0; i < loadedMenus.Count; i++)
        {
            m = loadedMenus[i];
            if (m == null)
            {
                loadedMenus.RemoveAt(i);
                i--;
            }
            else if (m.title == title)
            {
                return m;
            }
        }
        return null;
    }

    protected static void AddToLoadedMenus(MenuBase menu)
    {
        loadedMenus.Add(menu);
    }

    protected static bool TryRemoveFromLoaded(MenuBase menu)
    {
        if (menu == null)
        {
            SpesLogger.Warning("ѕопытка удалить null из загруженных меню");
            return false;
        }

        if (FindInLoadedMenus(menu.title))
        {
            RemoveFromLoaded(menu);
            return true;
        }

        SpesLogger.Detail("ћеню " + menu.title + " не было найдено в загруженных и не может быть удалено");
        return false;
    }

    protected static void RemoveFromLoaded(MenuBase menu)
    {
        loadedMenus.Remove(menu);
    }

    public static MenuBase OpenMenu(string title)
    {
        MenuBase m = GetFromLoaded(title);
        if (!m)
        {
            m = menus.GetPrefab(title);

            if (m != null)
                m = Instantiate(m);
        }

        return m;
    }

    public static void SetLibrary(MenusLibrary lib)
    {
        menus = lib;
    }

    #endregion

    #region Functions

    public void GoToMenu(string title)
    {
        gameObject.SetActive(false);
        var m = OpenMenu(title);
        m.gameObject.SetActive(true);
    }
    public virtual void Reset()
    {
    }

    #endregion

    #region UnityCallbacks

    public void Awake()
    {
        if (!FindInLoadedMenus(title))
            AddToLoadedMenus(this);
    }

    public void OnDestroy()
    {
        TryRemoveFromLoaded(this);
    }

    #endregion
}
