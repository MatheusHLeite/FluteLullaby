using Steamworks;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class Player_VisualManagementSystem : NetworkBehaviour {
    public SkinnedMeshRenderer[] body; //[TODO] Placeholder
    public GameObject deathCamera; //[TODO] Placeholder
    public TMP_Text nameIndicator; //[TODO] Placeholder
    public GameObject firstPersonHolder; //[TODO] Placeholder

    private Player_CameraMovementSystem CameraSystem;

    public NetworkVariable<FixedString64Bytes> PlayerName = new NetworkVariable<FixedString64Bytes>(writePerm: NetworkVariableWritePermission.Server);

    private void Awake() {
        CameraSystem = GetComponent<Player_CameraMovementSystem>();
    }

    public override void OnNetworkSpawn() {
        PlayerName.OnValueChanged += OnNameChanged;

        if (IsOwner) {
            Singleton.Instance.GameEvents.OnPlayerDie.AddListener(OnPlayerDie);
            Singleton.Instance.GameEvents.OnPlayerRespawn.AddListener(OnPlayerRespawn);
            Singleton.Instance.GameEvents.OnShotHit.AddListener(OnWeaponHit);

            nameIndicator.gameObject.SetActive(false);
            SetBodyVisible(false);
            firstPersonHolder.SetActive(true);

            if (GameNetworkManager.IsSteam) SubmitNameServerRpc(SteamClient.Name);
            return;
        }

        nameIndicator.gameObject.SetActive(true);
        SetBodyVisible(true);
        firstPersonHolder.SetActive(false);
    }

    public override void OnNetworkDespawn() {
        if (IsOwner) {
            Singleton.Instance.GameEvents.OnPlayerDie.RemoveListener(OnPlayerDie);
            Singleton.Instance.GameEvents.OnPlayerRespawn.RemoveListener(OnPlayerRespawn);
            Singleton.Instance.GameEvents.OnShotHit.RemoveListener(OnWeaponHit);
        }
    }

    public void OnNameChanged(FixedString64Bytes prevValue, FixedString64Bytes newValue) {
        nameIndicator.SetText(newValue.ToString());
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
        firstPersonHolder.SetActive(false);
        deathCamera.gameObject.SetActive(true);
        CameraSystem.SetCameraGameObjectActive(false);
    }

    private void OnPlayerRespawn() {
        SetBodyVisible(false);
        firstPersonHolder.SetActive(true);
        deathCamera.gameObject.SetActive(false);
        CameraSystem.SetCameraGameObjectActive(true);
    }    

    private void SetBodyVisible(bool visible) {
        foreach (var skin in body) {
            skin.enabled = visible;
        }
    }

    private void Update() {
        if (!IsOwner && Camera.main != null) {
            nameIndicator.transform.forward = Camera.main.transform.forward;
        }
    }
}
