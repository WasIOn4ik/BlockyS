using UnityEngine;
using Unity.Netcode;
using UnityEngine.Localization.Settings;
using UnityEngine.AddressableAssets;

[RequireComponent(typeof(ServerBase), typeof(ClientBase), typeof(GameStorage))]
public class GameBase : MonoBehaviour
{
    #region Variables

    [SerializeField] private MenusLibrary menusLibrary;
    public SkinsLibrary skins;
    public GameRules gameRules;
    public AssetReference messageMenuAsset;

    public bool bNetMode;

	private MessageScript currentMessage;

    #endregion

    #region StaticVariables

    public static GameBase instance;
    public static ServerBase server;
    public static ClientBase client;
    public static GameStorage storage;

    #endregion

    #region UnityCallbacks

    private void Awake()
    {
        if (instance)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(this);
        MenuBase.SetLibrary(menusLibrary);

        server = GetComponent<ServerBase>();
        client = GetComponent<ClientBase>();
        storage = GetComponent<GameStorage>();

        var net = GetComponent<NetworkManager>();

        server.networkManager = net;
        client.networkManager = net;

        Application.targetFrameRate = 60;
        Application.quitting += Application_OnQuit;

        storage.LoadPrefs();
        storage.LoadProgress();

        if (storage.CurrentBoardSkin != 0 && !storage.CheckBoard(storage.CurrentBoardSkin))
        {
            storage.CurrentBoardSkin = 0;
        }

        if (storage.CurrentPawnSkin != 0 && !storage.CheckPawn(storage.CurrentPawnSkin))
        {
            storage.CurrentPawnSkin = 0;
        }
    }

    #endregion

    #region Callbacks

    private void Application_OnQuit()
    {
        storage.SaveProgress();
        storage.SavePrefs();
    }

    #endregion

    #region Functions

    public void ApplyLanguage(string code)
    {
        var settings = LocalizationSettings.Instance;
        var locale = settings.GetAvailableLocales().GetLocale(code);
        settings.SetSelectedLocale(locale);
        SpesLogger.Detail("Current language set to " + code);
    }

    public void ShowMessage(string entry, MessageAction action, bool bLocalized, string param = "")
    {
        if (!currentMessage)
        {
            Addressables.InstantiateAsync(messageMenuAsset).Completed += (x) =>
            {
                if(x.Status != UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                    SpesLogger.Critical("Error while loading MessageAssetReference");

				currentMessage = x.Result.GetComponent<MessageScript>();

				currentMessage.ShowMessage(entry, action, bLocalized, param);
			};
            return;
        }

        currentMessage.ShowMessage(entry, action, bLocalized, param);
    }

    #endregion
}
