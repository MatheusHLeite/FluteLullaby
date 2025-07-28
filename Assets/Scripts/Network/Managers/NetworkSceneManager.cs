using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkSceneManager : NetworkBehaviour {
    [SerializeField] private GameObject m_playerPrefab;

    private void Awake() {
        Singleton.Instance.GameEvents.OnHostStarted.AddListener(LoadGameScene);
        Singleton.Instance.GameEvents.OnClientStarted.AddListener(LoadGameScene);
    }

    public override void OnDestroy() {
        Singleton.Instance.GameEvents.OnHostStarted.RemoveListener(LoadGameScene);
        Singleton.Instance.GameEvents.OnClientStarted.RemoveListener(LoadGameScene);
    }

    public override void OnNetworkSpawn() {
        NetworkManager.Singleton.SceneManager.OnLoadComplete += OnSceneLoaded;
    }

    public override void OnNetworkDespawn() {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnSceneLoaded;
    }

    private void OnSceneLoaded(ulong clientId, string sceneName, LoadSceneMode mode) {
        if (!NetworkManager.Singleton.IsServer ||
            !NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId) ||
            NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject != null) return;

        var player = Instantiate(m_playerPrefab, GetSpawnPosition(), Quaternion.identity);
        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);

        Singleton.Instance.GameEvents.OnGameStarted?.Invoke();
    }

    private Vector3 GetSpawnPosition() {
        return new Vector3(0, 1, 0);
    }

    public void LoadGameScene() {
        if (IsServer)
            NetworkManager.Singleton.SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
    }
}