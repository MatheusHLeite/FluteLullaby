using UnityEngine;
using UnityEngine.Events;

public class GameEvents : MonoBehaviour {
    public UnityEvent OnHostStarted { get; set; } = new();
    public UnityEvent OnClientStarted { get; set; } = new();
    public UnityEvent<string> OnHoverOverItem { get; set; } = new(); 
    public UnityEvent<Item_SO, int> OnSlotItemCollected { get; private set; } = new();
    public UnityEvent<int> OnSlotItemDropped { get; private set; } = new(); 
    public UnityEvent<int> OnSlotSelected { get; private set; } = new();
    public UnityEvent<Item_SO> OnActualSlotItem { get; private set; } = new();
    public UnityEvent<int, int> OnHealthSet { get; private set; } = new();
    public UnityEvent<float, float> OnDamageTaken { get; private set; } = new();
    public UnityEvent<Vector3, Vector3, float> OnPlayerDie { get; private set; } = new();
    public UnityEvent OnPlayerRespawn { get; private set; } = new();
    public UnityEvent<float, float> OnStaminaUsage { get; private set; } = new();
    public UnityEvent OnHit { get; private set; } = new();
    public UnityEvent OnKill { get; private set; } = new();
    public UnityEvent<float> OnSensitivityChange { get; private set; } = new();
    public UnityEvent<float> OnSensitivityInitiated { get; private set; } = new();
    public UnityEvent<string, int, int> OnAmmoConsumed { get; private set; } = new();
    public UnityEvent<Weapon_Firearm> OnWeaponChanged { get; private set; } = new();
    public UnityEvent<RaycastHit> OnShotHit { get; private set; } = new();
    public UnityEvent OnGameStarted { get; private set; } = new(); 
}