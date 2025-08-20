using UnityEngine;

public class Singleton : MonoBehaviour {
    public static Singleton Instance;

    public GameManager GameManager { get; private set; }
    public SaveManager SaveManager { get; private set; }
    public GameEvents GameEvents { get; private set; }    
    public InputHandler InputHandler { get; private set; }
    public NetworkSceneManager NetworkSceneManager { get; private set; }
    public VFXManager VFXManager { get; private set; }
    public InventoryManager InventoryManager { get; private set; }
    public SettingsManager SettingsManager { get; private set; }

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
        SaveManager = GetComponent<SaveManager>();
        GameEvents = GetComponent<GameEvents>();
        InputHandler = GetComponent<InputHandler>();
        NetworkSceneManager = GetComponent<NetworkSceneManager>();
        VFXManager = GetComponent<VFXManager>();
        InventoryManager = GetComponent<InventoryManager>();
        SettingsManager = GetComponent<SettingsManager>();
    }
}
