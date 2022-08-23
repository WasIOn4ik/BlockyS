using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Spes/MenusLibrary")]
public class MenusLibrary : ScriptableObject
{
    public List<MenuBase> prefabs = new List<MenuBase>();

    public MenuBase GetPrefab(string title)
    {
        var m = prefabs.Find(x => { return x.title == title; });

        if (!m)
            SpesLogger.Error("ѕопытка получить Prefab меню с именем " + title + ", но оно не найдено в MenusLibrary");

        return m;
    }
}
