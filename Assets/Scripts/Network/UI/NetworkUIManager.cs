using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkUIManager : MonoBehaviour
{
    [SerializeField] private Button btn_host;

    private void Awake()  {
        Singleton.Instance.GameEvents.OnHostStarted.AddListener(OnGameStarted);
        Singleton.Instance.GameEvents.OnClientStarted.AddListener(OnGameStarted);
        btn_host.onClick.AddListener(() => {
            StartHost();
        });
    }

    private void OnDestroy() {
        Singleton.Instance.GameEvents.OnHostStarted.RemoveListener(OnGameStarted);
        Singleton.Instance.GameEvents.OnClientStarted.RemoveListener(OnGameStarted);
        btn_host.onClick.RemoveListener(() => {
            StartHost();
        });
    }

    private void StartHost() {
        GameNetworkManager.Instance.StartHost();
    }

    private void OnGameStarted() {
        Destroy(gameObject);
    }
}