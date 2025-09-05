using UnityEngine;

public class NPC_Manager : MonoBehaviour {
    private Rigidbody rgbd;
    private void Awake() {
        rgbd = GetComponent<Rigidbody>();
    }

    public void OnDie(Vector3 hitDirection, float impact) {
        rgbd.isKinematic = false;
        rgbd.AddForce(hitDirection * impact, ForceMode.Impulse);
    }
}
