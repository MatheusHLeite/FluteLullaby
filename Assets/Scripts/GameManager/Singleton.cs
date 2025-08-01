using UnityEngine;

public class Singleton : MonoBehaviour {
    public static Singleton Instance;

    public GameManager GameManager { get; private set; }
    public GameEvents GameEvents { get; private set; }    
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

        GameManager = GetComponent<GameManager>();
        GameEvents = GetComponent<GameEvents>();        
        NetworkSceneManager = GetComponent<NetworkSceneManager>();
        VFXManager = GetComponent<VFXManager>();
    }
}
