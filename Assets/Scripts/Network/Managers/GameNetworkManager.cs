using Netcode.Transports.Facepunch;
using Steamworks;
using Steamworks.Data;
using Unity.Netcode;
using UnityEngine;

public class GameNetworkManager : MonoBehaviour {
    public static GameNetworkManager Instance { get; private set; }

    public Lobby? CurrentLobby { get; private set; } = null;

    private FacepunchTransport _transport = null;
    [SerializeField] private int _maxPlayers = 2; //Modders will break this xD [TODO] Back to 2 players

    public static bool IsSteam;
    private bool connected;

    #region Initialization
    private void Awake() {
        if (Instance != null) {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        Instance = this;
        _transport = GetComponent<FacepunchTransport>();

        CheckConnectionType();
    }

    private void Start() {        
        AddListeners();
    }

    private void OnDestroy() {
        RemoveListeners();
    }

    private void OnApplicationQuit() => Disconnect();

    private void CheckConnectionType() {
        IsSteam = _transport != null;
    }

    private void AddListeners() {
        SteamMatchmaking.OnLobbyCreated += OnLobbyCreated;
        SteamMatchmaking.OnLobbyEntered += OnLobbyEntered;
        SteamMatchmaking.OnLobbyMemberJoined += OnLobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave += OnLobbyMemberLeave;
        SteamMatchmaking.OnLobbyInvite += OnLobbyInvite;
        SteamMatchmaking.OnLobbyGameCreated += OnLobbyGameCreated;
        SteamFriends.OnGameLobbyJoinRequested += OnGameLobbyJoinRequested;
    }

    private void RemoveListeners() {
        SteamMatchmaking.OnLobbyCreated -= OnLobbyCreated;
        SteamMatchmaking.OnLobbyEntered -= OnLobbyEntered;
        SteamMatchmaking.OnLobbyMemberJoined -= OnLobbyMemberJoined;
        SteamMatchmaking.OnLobbyMemberLeave -= OnLobbyMemberLeave;
        SteamMatchmaking.OnLobbyInvite -= OnLobbyInvite;
        SteamMatchmaking.OnLobbyGameCreated -= OnLobbyGameCreated;
        SteamFriends.OnGameLobbyJoinRequested -= OnGameLobbyJoinRequested;

        if (!NetworkManager.Singleton) return;

        NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
    }
    #endregion

    #region Steam callbacks
    private void OnLobbyCreated(Result result, Lobby lobby) { 
        if (result != Result.OK) {
            Debug.LogError($"Lobby couldn't be created, {result}", this);
            return;
        }

        lobby.SetFriendsOnly();
        lobby.SetData("lobbyName", $"{lobby.Owner.Name}'s lobby");
        lobby.SetJoinable(true);

        Debug.Log("Lobby has been created", this);
    }

    private void OnLobbyEntered(Lobby lobby) {
        if (NetworkManager.Singleton.IsHost) return;

        StartClient(lobby.Id);
    }

    private void OnLobbyMemberJoined(Lobby lobby, Friend friend) { }

    private void OnLobbyMemberLeave(Lobby lobby, Friend friend) { }

    private void OnLobbyInvite(Friend friend, Lobby lobby) => Debug.Log($"{friend.Name} has invited you!", this);

    private void OnLobbyGameCreated(Lobby lobby, uint ip, ushort port, SteamId id) { }

    private void OnGameLobbyJoinRequested(Lobby lobby, SteamId id) => StartClient(id);
    #endregion

    #region Netcode callbacks
    private void OnServerStarted() => Debug.Log("Server started", this);

    private void OnClientConnectedCallback(ulong clientID) => Debug.Log($"Player connected | {clientID}");    

    private void OnClientDisconnectCallback(ulong clientID) {
        Debug.Log($"Player disconnected | {clientID}");

        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
    }
    #endregion

    public async void StartHost() {
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;       

        NetworkManager.Singleton.StartHost();

        CurrentLobby = await SteamMatchmaking.CreateLobbyAsync(_maxPlayers);

        Singleton.Instance.GameEvents.OnHostStarted?.Invoke();
    }

    public void StartClient(SteamId id) {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;

        _transport.targetSteamId = id;

        NetworkManager.Singleton.StartClient();

        Singleton.Instance.GameEvents.OnClientStarted?.Invoke();
    }

    public void Disconnect() {
        CurrentLobby?.Leave();

        if (!NetworkManager.Singleton) return;

        NetworkManager.Singleton.Shutdown();
    }

    private void Update() {
        if (connected) return;

        if (IsSteam) {
            if (Input.GetKeyDown(KeyCode.H)) {
                StartHost();
                connected = true;
            }
            return;
        }

        if (Input.GetKeyDown(KeyCode.H)) {
            NetworkManager.Singleton.StartHost();
            Singleton.Instance.GameEvents.OnHostStarted?.Invoke();
            connected = true;
        }
            
        if (Input.GetKeyDown(KeyCode.C)) {
            NetworkManager.Singleton.StartClient();
            Singleton.Instance.GameEvents.OnClientStarted?.Invoke();
            connected = true;
        }           
    }
}