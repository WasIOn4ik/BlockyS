using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
	#region Enums

	public enum SoundType
	{
		Button,
		BackButton,
		PawnMove,
		WallPlace,
		Win,
		Lose
	}

	public enum MusicType
	{
		MainMenuMusic,
		GameMusic
	}

	#endregion

	#region Variables

	[Header("Properties")]
	[SerializeField] private float fadeMultiplier = 0.03f;

	[Header("Components")]
	[SerializeField] private AudioSource audioSource;
	[SerializeField] private SoundsSO soundsSO;


	public float effectsVolume = 1f;
	public float musicVolume = 1f;

	public static SoundManager Instance { get; private set; }

	#endregion

	#region UnityCallbacks

	private void Awake()
	{
		if (Instance == null)
			Instance = this;
	}

	private void Start()
	{
		GameBase.Storage.onEffectsVolumeChanged += GameBase_Storage_onEffectsVolumeChanged;
		GameBase.Storage.onMusicVolumeChanged += Storage_onMusicVolumeChanged;
		effectsVolume = GameBase.Storage.EffectsVolume;
		musicVolume = GameBase.Storage.MusicVolume;
		audioSource.volume = musicVolume;
	}

	#endregion

	#region Callbacks

	private void GameBase_Storage_onEffectsVolumeChanged(object sender, GameStorage.FloatEventArgs e)
	{
		effectsVolume = e.value;
	}

	private void Storage_onMusicVolumeChanged(object sender, GameStorage.FloatEventArgs e)
	{
		musicVolume = e.value;
		audioSource.volume = musicVolume;
	}

	#endregion

	#region Functions

	public void PlayButtonClick()
	{
		AudioSource.PlayClipAtPoint(soundsSO.buttonClip, Camera.main.transform.position, effectsVolume);
	}

	public void PlayBackButtonClick()
	{
		AudioSource.PlayClipAtPoint(soundsSO.backClip, Camera.main.transform.position, effectsVolume);
	}

	public void PlayPawnMove()
	{
		AudioSource.PlayClipAtPoint(soundsSO.pawnMoveClip, Camera.main.transform.position, effectsVolume);
	}

	public void PlayWallPlace()
	{
		AudioSource.PlayClipAtPoint(soundsSO.wallPlaceClip, Camera.main.transform.position, effectsVolume);
	}

	public void PlayWin()
	{
		AudioSource.PlayClipAtPoint(soundsSO.winClip, Camera.main.transform.position, effectsVolume);
	}

	public void PlayLose()
	{
		AudioSource.PlayClipAtPoint(soundsSO.loseClip, Camera.main.transform.position, effectsVolume);
	}

	public void StartBackgroundMusic(MusicType musicType)
	{
		AudioClip clipToPlay = audioSource.clip;

		if (clipToPlay != null)
		{
			switch (musicType)
			{
				case MusicType.MainMenuMusic:
					StopAllCoroutines();
					StartCoroutine(SwitchMusic(soundsSO.menuMusic));
					break;
				case MusicType.GameMusic:
					StopAllCoroutines();
					StartCoroutine(SwitchMusic(soundsSO.gameMusic[Random.Range(0, soundsSO.gameMusic.Length)]));
					break;
			}
		}
		else
		{
			switch (musicType)
			{
				case MusicType.MainMenuMusic:
					audioSource.clip = soundsSO.menuMusic;
					audioSource.Play();
					break;
				case MusicType.GameMusic:
					audioSource.clip = soundsSO.gameMusic[Random.Range(0, soundsSO.gameMusic.Length)];
					audioSource.Play();
					break;
			}
		}
	}

	public void StopBackgroundMusic()
	{
		audioSource.Stop();
		audioSource.clip = null;
	}

	public IEnumerator HandleMusic()
	{
		audioSource.volume = 0;

		while (audioSource.volume < musicVolume)
		{
			audioSource.volume += fadeMultiplier * musicVolume;
			yield return null;
		}

		audioSource.volume = musicVolume;
	}

	private IEnumerator SwitchMusic(AudioClip clip)
	{
		Debug.Log("Started");

		while (audioSource.volume > 0)
		{
			audioSource.volume -= fadeMultiplier * musicVolume;
			yield return null;
		}

		audioSource.clip = clip;
		audioSource.Play();

		yield return new WaitForSeconds(0.4f);

		yield return HandleMusic();
	}

	#endregion
}
