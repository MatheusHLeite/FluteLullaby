using Unity.Netcode;
using UnityEngine;

public class Player_BodyPart : MonoBehaviour {
    [SerializeField] private BodyPart bodyPart;

    private Player_HealthSystem healthSystem;
    private GameManager gameManager;

    private void Awake() {
        healthSystem = transform.root.GetComponent<Player_HealthSystem>();

        gameManager = Singleton.Instance.GameManager;
    }

    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitDirection, float impact) => healthSystem.TakeDamage(damage * gameManager.GetDamageMultiplier(bodyPart), hitPoint, hitDirection, impact);
}
