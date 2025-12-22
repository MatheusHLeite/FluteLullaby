using UnityEngine;

public class Weapon_Shotgun : Weapon_Firearm {
    [SerializeField] private int pelletCount = 8;
    [SerializeField] private float spreadAngle = 5;
    
    #region Private
    private Vector2 spreadOffset;
    private Vector3 right;
    private Vector3 up;
    private Vector3 spreadDirection;
    #endregion

    protected override void Fire() {
        OnShot();

        AudioSystem.CallPlayShotSFX(Weapons.Shotgun);

        for (int i = 0; i < pelletCount; i++) {
            Vector3 direction = GetSpreadDirection();
            Physics.Raycast(CameraMovement.GetPlayerCamera.transform.position, direction, out hit, m_range, ~layerToIgnore);

            if (hit.collider != null && hit.collider.TryGetComponent(out Damagable_BodyPart damagable)) 
                damagable.TakeDamage(m_damage / pelletCount, hit.point, ray.direction, m_impact);            

            Singleton.Instance.GameEvents.OnShot?.Invoke(weaponMuzzle.position, hit, direction);
        }
    }

    Vector3 GetSpreadDirection() {
        spreadOffset = Random.insideUnitCircle * Mathf.Tan(spreadAngle * Mathf.Deg2Rad);

        right = CameraMovement.GetPlayerCamera.transform.right;
        up = CameraMovement.GetPlayerCamera.transform.up;

        spreadDirection = ray.direction + spreadOffset.x * right + spreadOffset.y * up;
        return spreadDirection.normalized;
    }
}