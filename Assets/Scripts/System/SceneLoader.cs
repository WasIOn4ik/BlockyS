using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum Scenes
{
	StartupScene,
	LobbyScene,
	GameScene
}

public class SceneLoader
{
	public static void LoadScene(Scenes scene, LoadSceneMode mode = LoadSceneMode.Single)
	{
		SceneManager.LoadScene(scene.ToString(), mode);
	}

	public static void LoadNetwork(Scenes scene, LoadSceneMode mode = LoadSceneMode.Single)
	{
		NetworkManager.Singleton.SceneManager.LoadScene(scene.ToString(), mode);
	}
}
