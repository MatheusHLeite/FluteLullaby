using System;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Vivox;

public class NetworkInitializationManager : MonoBehaviour {
    //public static NetworkInitializationManager Instance { get; private set; }

    public uint YourAppId;

    void Awake()
    {
        /*if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            try
            {
                SteamClient.Init(YourAppId, true);
                if (!SteamClient.IsValid)
                    Debug.LogError("SteamClient inválido");
            }
            catch (System.Exception e)
            {
                Debug.LogError("Falha ao inicializar SteamClient: " + e);
            }
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }*/
    }

    async void InitializeAsync()
    {
        /*await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        await VivoxService.Instance.InitializeAsync();*/

        
    }

    public async void LoginToVivoxAsync()
    {
        //await VivoxService.Instance.LoginAsync(LoginOptions options = null);
    }

    public async void JoinEchoChannelAsync() {
        string channelToJoin = "Lobby";
        await VivoxService.Instance.JoinEchoChannelAsync(channelToJoin, ChatCapability.TextAndAudio);
    }
}
