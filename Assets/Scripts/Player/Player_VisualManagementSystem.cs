using Steamworks;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class Player_VisualManagementSystem : NetworkBehaviour {
    [Header("Visuals")]
    [SerializeField] private SkinnedMeshRenderer[] m_body;
    [SerializeField] private GameObject m_deathCamera;
    [SerializeField] private GameObject m_firstPersonHolder;

    [Header("Identification")]
    [SerializeField] private TMP_Text m_nameIndicator;
    [SerializeField] private GameObject m_indicator;
    [SerializeField] private GameObject m_indicatorHolder;

    private Player_CameraMovementSystem CameraSystem;
    public NetworkVariable<FixedString64Bytes> PlayerName = new NetworkVariable<FixedString64Bytes>(writePerm: NetworkVariableWritePermission.Server);

    private void Awake() {
        CameraSystem = GetComponent<Player_CameraMovementSystem>();
    }

    public override void OnNetworkSpawn() {
        PlayerName.OnValueChanged += OnNameChanged;

        Singleton.Instance.GameEvents.OnPlayerIndicatorChanged.AddListener(OnPlayerIndicatorChanged);

        if (IsOwner) {
            Singleton.Instance.GameEvents.OnPlayerDie.AddListener(OnPlayerDie);
            Singleton.Instance.GameEvents.OnPlayerRespawn.AddListener(OnPlayerRespawn);
            Singleton.Instance.GameEvents.OnShotHit.AddListener(OnWeaponHit);

            m_indicatorHolder.gameObject.SetActive(false);
            SetBodyVisible(false);
            m_firstPersonHolder.SetActive(true);

            if (GameNetworkManager.IsSteam) SubmitNameServerRpc(SteamClient.Name);
            return;
        }

        m_indicatorHolder.gameObject.SetActive(true);
        SetBodyVisible(true);
        m_firstPersonHolder.SetActive(false);
    }

    public override void OnNetworkDespawn() {
        Singleton.Instance.GameEvents.OnPlayerIndicatorChanged.RemoveListener(OnPlayerIndicatorChanged);

        if (IsOwner) {
            Singleton.Instance.GameEvents.OnPlayerDie.RemoveListener(OnPlayerDie);
            Singleton.Instance.GameEvents.OnPlayerRespawn.RemoveListener(OnPlayerRespawn);
            Singleton.Instance.GameEvents.OnShotHit.RemoveListener(OnWeaponHit); 
        }
    }

    private void OnPlayerIndicatorChanged(int index) {
        bool nameIndicator = true;
        bool indicator = true;

        switch (index) {
            case 0:
                nameIndicator = true;
                indicator = true;
                break;
            case 1:
                nameIndicator = true;
                indicator = false;
                break;
            case 2:
                nameIndicator = false;
                indicator = true;
                break;
            case 3:
                nameIndicator = false;
                indicator = false;
                break;
        }

        m_nameIndicator.gameObject.SetActive(nameIndicator);
        m_indicator.gameObject.SetActive(indicator);
    }

    public void OnNameChanged(FixedString64Bytes prevValue, FixedString64Bytes newValue) {
        m_nameIndicator.SetText(newValue.ToString());
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitNameServerRpc(string name) {
        PlayerName.Value = name;
    }

    private void OnWeaponHit(RaycastHit hit) {
        if (hit.collider.GetComponent<Player_BodyPart>() || hit.collider.GetComponent<Enemy>()) return;

        GameObject newDecal = Singleton.Instance.VFXManager.GetShotDecal();
        newDecal.transform.position = hit.point;
        newDecal.transform.rotation = Quaternion.LookRotation(hit.normal);
        newDecal.SetActive(true);

        Singleton.Instance.VFXManager.ReturnShotDecal(newDecal);
    }

    private void OnPlayerDie(Vector3 point, Vector3 dir, float impact) {
        SetBodyVisible(true);
        m_firstPersonHolder.SetActive(false);
        m_deathCamera.gameObject.SetActive(true);
        CameraSystem.SetCameraGameObjectActive(false);
    }

    private void OnPlayerRespawn() {
        SetBodyVisible(false);
        m_firstPersonHolder.SetActive(true);
        m_deathCamera.gameObject.SetActive(false);
        CameraSystem.SetCameraGameObjectActive(true);
    }    

    private void SetBodyVisible(bool visible) {
        foreach (var skin in m_body) {
            skin.enabled = visible;
        }
    }

    private void Update() {
        if (!IsOwner && Camera.main != null) {
            m_indicatorHolder.transform.forward = Camera.main.transform.forward;
        }
    }
}
