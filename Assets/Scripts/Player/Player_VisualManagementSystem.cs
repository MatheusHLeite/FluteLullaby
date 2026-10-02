using Steamworks;
using System.Collections;
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

    [Header("VFX")]
    [SerializeField] private GameObject m_parryVFX;
    [SerializeField] private GameObject m_defenseBrokenVFX;

    private enum VisualEffectsType { ParryFX, DefenseFX }

    private Player_CameraMovementSystem CameraSystem;
    private NetworkVariable<FixedString64Bytes> PlayerName = new NetworkVariable<FixedString64Bytes>(writePerm: NetworkVariableWritePermission.Server);

    private void Awake() {
        CameraSystem = GetComponent<Player_CameraMovementSystem>();
    }

    public void InitializeNetwork(bool isOwner) {
        PlayerName.OnValueChanged += OnNameChanged;

        Singleton.Instance.GameEvents.OnPlayerIndicatorChanged.AddListener(OnPlayerIndicatorChanged);

        if (isOwner) {
            Singleton.Instance.GameEvents.OnPlayerDie.AddListener(OnPlayerDie);
            Singleton.Instance.GameEvents.OnPlayerRespawn.AddListener(OnPlayerRespawn);
            Singleton.Instance.GameEvents.OnShot.AddListener(OnShot);

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

    public void DeinitializeNetwork(bool isOwner) {
        Singleton.Instance.GameEvents.OnPlayerIndicatorChanged.RemoveListener(OnPlayerIndicatorChanged);

        if (!isOwner) return;

        Singleton.Instance.GameEvents.OnPlayerDie.RemoveListener(OnPlayerDie);
        Singleton.Instance.GameEvents.OnPlayerRespawn.RemoveListener(OnPlayerRespawn);
        Singleton.Instance.GameEvents.OnShot.RemoveListener(OnShot);
    }

    #region VFX
    private void OnShot(Vector3 initialPoint, RaycastHit targetPos, Vector3 direction) {
        TrailRenderer newTrail = Singleton.Instance.VFXManager.GetShotTrail();        
        newTrail.transform.rotation = Quaternion.LookRotation(direction);
        newTrail.transform.position = initialPoint - newTrail.transform.forward;
        newTrail.gameObject.SetActive(true);

        StartCoroutine(NewTrail(newTrail));

        if (targetPos.point != Vector3.zero) 
            OnWeaponHit(targetPos);        
    }

    private void OnWeaponHit(RaycastHit hit) {
        if (hit.collider.GetComponent<Damagable_BodyPart>()) {
            ParticleSystem bloodVFX = Singleton.Instance.VFXManager.GetBloodSplatFX();
            bloodVFX.transform.position = hit.point;
            bloodVFX.transform.rotation = Quaternion.LookRotation(hit.normal);
            bloodVFX.gameObject.SetActive(true);
            bloodVFX.Play();

            Singleton.Instance.VFXManager.ReturnBloodSplatFX(bloodVFX);
            return; 
        }

        GameObject newDecal = Singleton.Instance.VFXManager.GetShotDecal();
        newDecal.transform.position = hit.point;
        newDecal.transform.rotation = Quaternion.LookRotation(hit.normal);
        newDecal.SetActive(true);

        Singleton.Instance.VFXManager.ReturnShotDecal(newDecal);
    }

    private IEnumerator NewTrail(TrailRenderer trail) {
        float time = 0;
        float newScale = 1;
        Vector3 scale = Vector3.one;
        trail.transform.localScale = Vector3.one;

        while (time < trail.time) {
            trail.transform.position += trail.transform.forward * 90f * Time.deltaTime;
            trail.transform.localScale = Vector3.Lerp(trail.transform.localScale, scale, time / Time.deltaTime);
            time += Time.deltaTime / trail.time;
            newScale -= Time.deltaTime / trail.time;
            scale = new Vector3(newScale, newScale, newScale);
            yield return null;
        }

        Singleton.Instance.VFXManager.ReturnTrail(trail);
    }

    public void OnParry(Vector3 pos) {
        Singleton.Instance.GlobalTimeManager.TriggerParrySlowMotion(2f);
        RequestVFXSpawn(pos, VisualEffectsType.ParryFX);
    }

    public void OnDefenseBroken(Vector3 pos) {
        RequestVFXSpawn(pos, VisualEffectsType.DefenseFX);
    }

    private void RequestVFXSpawn(Vector3 pos, VisualEffectsType visualEffectsType) {
        if (IsServer)
            PlayVFXRpc(pos, visualEffectsType);
        else
            RequestVFXSpawnRpc(pos, visualEffectsType);
    }

    [Rpc(SendTo.Server)]
    private void RequestVFXSpawnRpc(Vector3 position, VisualEffectsType visualEffectsType) => PlayVFXRpc(position, visualEffectsType);

    [Rpc(SendTo.ClientsAndHost)]
    private void PlayVFXRpc(Vector3 position, VisualEffectsType visualEffectsType) {
        GameObject correctFX = GetCorrectFX(visualEffectsType);

        if (correctFX == null)
            return;

        GameObject vfxInstance = Instantiate(correctFX, position, Quaternion.LookRotation(transform.forward));
        Destroy(vfxInstance, 3f);
    }

    private GameObject GetCorrectFX(VisualEffectsType visualEffectsType) {
        return visualEffectsType switch {
            VisualEffectsType.DefenseFX => m_defenseBrokenVFX,
            VisualEffectsType.ParryFX => m_parryVFX,
            _ => null
        };
    }
    #endregion

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

    public void Tick(bool isOwner) {
        if (isOwner) return;
        if (Camera.main == null) return;

        m_indicatorHolder.transform.forward = Camera.main.transform.forward;
    }
}
