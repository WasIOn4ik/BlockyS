using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class MenuBase : MonoBehaviour
{
	#region Constants

	public const string CREATE_LOBBY_MENU = "HostMenu";
	public const string CONNECT_TO_LOBBY_MENU = "ConnectMenu";
	public const string CREDITS_MENU = "CreditsMenu";
	public const string CUSTOMIZATION_MENU = "CustomizationMenu";
	public const string SETTINGS_MENU = "SettingsMenu";
	public const string STARTUP_MENU = "StartupMenu";
	public const string WAITING_FOR_PLAYERS_MENU = "WaitingMenu";

	#endregion

	#region Variables

	/// <summary>
	/// Menu title in search
	/// </summary>
	[SerializeField] public string title;

	/// <summary>
	/// Previous menu, that can be reopened by "Back" button
	/// </summary>
	protected MenuBase previousMenu;

	#endregion

	#region StaticVariables

	/// <summary>
	/// Loaded menus, that can be used without loading
	/// </summary>
	protected static List<MenuBase> loadedMenus = new List<MenuBase>();

	protected static MenuBase lastOpenedMenu;

	/// <summary>
	/// Must be assigned before first use
	/// </summary>
	protected static MenusLibrary menus;

	#endregion

	#region StaticFunctions

	protected static bool IsMenuInstantiated(string title)
	{
		foreach (var m in loadedMenus)
		{
			if (m)
				if (m.title == title)
					return true;
		}

		return false;
	}

	protected static MenuBase GetFromInstantiated(string title)
	{
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

	protected static void AddToInstantiatedMenus(MenuBase menu)
	{
		loadedMenus.Add(menu);
	}

	protected static bool TryRemoveFromInstantiatedMenus(MenuBase menu)
	{
		if (menu == null)
		{
			return false;
		}

		if (IsMenuInstantiated(menu.title))
		{
			RemoveFromInstantiatedMenus(menu);
			return true;
		}

		return false;
	}

	protected static void RemoveFromInstantiatedMenus(MenuBase menu)
	{
		loadedMenus.Remove(menu);
	}

	/// <summary>
	/// Tries to find loaded menu or creates new one
	/// </summary>
	/// <param name="title"></param>
	/// <returns></returns>
	public static void OpenMenu(string title, Action<MenuBase> onOpened = null)
	{
		if (lastOpenedMenu)
			lastOpenedMenu.HideMenu();

		MenuBase m = GetFromInstantiated(title);
		if (!m)
		{
			menus.LoadMenuPrefab(title, x => { InstantiateOnLoad(x, out m); onOpened?.Invoke(m); });
			return;
		}

		m.previousMenu = lastOpenedMenu;

		m.ShowMenu();

		onOpened?.Invoke(m);
	}

	public static void PreloadMenu(string title)
	{
		menus.LoadMenuPrefab(title, null);
	}

	/// <summary>
	/// Returns true if was closed
	/// </summary>
	/// <param name="title"></param>
	/// <returns></returns>
	public static bool CloseMenu(string title)
	{
		var m = GetFromInstantiated(title);
		if (!m)
			return false;

		m.CloseMenu();
		return true;
	}

	/// <summary>
	/// Must be assigned before first use
	/// </summary>
	/// <param name="lib"></param>
	public static void SetLibrary(MenusLibrary lib)
	{
		menus = lib;
	}

	private static void InstantiateOnLoad(MenuBase m, out MenuBase resM)
	{
		resM = Instantiate(m);

		resM.previousMenu = lastOpenedMenu;

		resM.ShowMenu();
	}

	#endregion

	#region Functions

	protected void GoToMenu(string title, bool bCloseCurrent = true)
	{
		if (bCloseCurrent)
			HideMenu();

		OpenMenu(title);
	}

	public void BackToPreviousMenu()
	{
		if (previousMenu)
		{
			previousMenu.ShowMenu();
			HideMenu();
		}
		else
		{
			SpesLogger.Warning($"Trying to back to null previous menu from {title}");
		}
	}

	public void ShowMenu()
	{
		if (previousMenu)
			previousMenu.HideMenu();

		gameObject.SetActive(true);
		lastOpenedMenu = this;
	}

	public void HideMenu()
	{
		gameObject.SetActive(false);
	}

	public void CloseMenu()
	{
		OnClose();
		Destroy(gameObject);
	}

	public virtual void Reset()
	{

	}

	protected virtual void OnClose()
	{

	}

	#endregion

	#region UnityCallbacks

	protected virtual void Awake()
	{
		if (!IsMenuInstantiated(title))
			AddToInstantiatedMenus(this);

		/*foreach (var m in loadedMenus)
		{
			if (m != this)
			{
				CloseMenu(m.title);
			}
		}*/
	}

	private void OnDestroy()
	{
		TryRemoveFromInstantiatedMenus(this);
		OnClose();
	}

	#endregion
}
