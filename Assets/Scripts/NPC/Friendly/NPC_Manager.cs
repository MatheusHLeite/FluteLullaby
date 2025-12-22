using UnityEngine;

public class NPC_Manager : MonoBehaviour {
    private Rigidbody rgbd;
    private Global_HealthHandler healthHandler;
    private void Awake() {
        rgbd = GetComponent<Rigidbody>();
        GetComponent<Global_HealthHandler>().m_onDie.AddListener(OnDie);
    }

    private void OnDie(Vector3 hitDirection, float impact) {
        rgbd.isKinematic = false;
        rgbd.AddForce(hitDirection * impact, ForceMode.Impulse);
    }
}
