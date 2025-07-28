using UnityEngine;

public class Singleton : MonoBehaviour {
    public static Singleton Instance;

    public GameEvents GameEvents { get; private set; }
    public GameManager GameManager { get; private set; }
    public NetworkSceneManager NetworkSceneManager { get; private set; }
    public VFXManager VFXManager { get; private set; }

    private void Awake() {
        if (Instance) {
            Destroy(gameObject);
            return;
        }

        InitiateReferences();
    }

    private void InitiateReferences() {
        Instance = this;

        DontDestroyOnLoad(gameObject);

        GameEvents = GetComponent<GameEvents>();
        GameManager = GetComponent<GameManager>();
        NetworkSceneManager = GetComponent<NetworkSceneManager>();
        VFXManager = GetComponent<VFXManager>();
    }
}
