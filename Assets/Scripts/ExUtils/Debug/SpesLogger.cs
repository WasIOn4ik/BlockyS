using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SpesLogLevel
{
	Debug,
	Detailed,
	Warning,
	Error,
	Critical
}

public class SpesLogger : MonoBehaviour
{
	#region StaticVariables

	protected static Color debugColor = new Color(0.75f, 0.75f, 0.75f), detailedColor = Color.white, warningColor = Color.yellow, errorColor = new Color(1f, 0.27f, 0f), criticalColor = new Color(0.5f, 0f, 0f);

	public static SpesLogLevel currentLevel = SpesLogLevel.Debug;

	#endregion

	#region StaticFunctions

	public static void Deb(string msg)
	{
		if (currentLevel > SpesLogLevel.Debug)
			return;

		Print(msg, debugColor);
	}

	public static void Detail(string msg)
	{
		if (currentLevel > SpesLogLevel.Detailed)
			return;

		Print(msg, detailedColor);
	}

	public static void Warning(string msg)
	{
		if (currentLevel > SpesLogLevel.Warning)
			return;

		PrintWarning(msg, warningColor);
	}

	public static void Error(string msg)
	{
		if (currentLevel > SpesLogLevel.Error)
			return;

		PrintError(msg, errorColor);
	}

	public static void Critical(string msg)
	{
		PrintError(msg, criticalColor);
	}

	public static void Exception(Exception ex, string msg = "")
	{
		PrintException(ex, msg);
	}

	protected static void Print(string msg, Color col)
	{
		print("<color=#" + ColorUtility.ToHtmlStringRGB(col) + ">" + msg + "</color>");
	}

	protected static void PrintWarning(string msg, Color col)
	{
		Debug.LogWarning("<color=#" + ColorUtility.ToHtmlStringRGB(col) + ">" + msg + "</color>");
	}

	protected static void PrintError(string msg, Color col)
	{
		Debug.LogError("<color=#" + ColorUtility.ToHtmlStringRGB(col) + ">" + msg + "</color>");
	}

	protected static void PrintException(Exception ex, string msg)
	{
		Debug.LogException(ex);
	}

	#endregion
}
