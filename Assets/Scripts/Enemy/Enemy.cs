using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour {
    [SerializeField] private Material m_onHitMaterial; //[TODO]
    [SerializeField] private Renderer m_enemySkin;
    [Range(0, 255)] public byte m_onHitEffectIntensity;
    public float m_hp;

    private Color32 color;
    private Color32 noAlphaColor;

    private int hitMaterialIndex = -1;

    private MaterialPropertyBlock propBlock;
    private Material materialHitInstance;

    private Coroutine damageFlashCoroutine;

    private void Start() {
        SetMaterials();
    }

    private void OnDestroy() {
        if (materialHitInstance) Destroy(materialHitInstance);
    }

    private void SetMaterials() {
        propBlock = new MaterialPropertyBlock();

        noAlphaColor = Color.yellow;
        color = Color.red;

        noAlphaColor.a = 0;
        color.a = m_onHitEffectIntensity;
        
        Material[] currentMaterials = m_enemySkin.materials;

        for (int i = 0; i < currentMaterials.Length; i++) {
            if (currentMaterials[i].name.Contains(m_onHitMaterial.name)) {
                hitMaterialIndex = i;
                materialHitInstance = currentMaterials[i];
                break;
            }
        }

        if (hitMaterialIndex == -1) {
            Material[] newMats = new Material[currentMaterials.Length + 1];
            for (int i = 0; i < currentMaterials.Length; i++)
                newMats[i] = currentMaterials[i];

            if (!materialHitInstance)
                materialHitInstance = new Material(m_onHitMaterial);
            newMats[^1] = materialHitInstance;
            hitMaterialIndex = newMats.Length - 1;

            m_enemySkin.materials = newMats;
        }

        m_enemySkin.GetPropertyBlock(propBlock, hitMaterialIndex);
        propBlock.SetColor("_BaseColor", noAlphaColor);
        m_enemySkin.SetPropertyBlock(propBlock, hitMaterialIndex);
    }

    public void TakeDamage(float damage) {
        print($"dealing {damage} to {gameObject.name}");

        VisualHit();

        Singleton.Instance.GameEvents.OnHit?.Invoke();

        m_hp -= damage;
        if (m_hp <= 0) {
            Singleton.Instance.GameEvents.OnKill?.Invoke();
            Destroy(gameObject);
        }
    }

    private void VisualHit() {
        if (damageFlashCoroutine != null)
            StopCoroutine(damageFlashCoroutine);

        m_enemySkin.GetPropertyBlock(propBlock, hitMaterialIndex);
        propBlock.SetColor("_BaseColor", color);
        m_enemySkin.SetPropertyBlock(propBlock, hitMaterialIndex);

        damageFlashCoroutine = StartCoroutine(FlashEffect(0.15f));
    }

    private IEnumerator FlashEffect(float duration) {
        float elapsed = 0f;

        while (elapsed < duration) {
            float t = elapsed / duration;
            Color lerpedColor = Color.Lerp(color, noAlphaColor, t);
            propBlock.SetColor("_BaseColor", lerpedColor);
            m_enemySkin.SetPropertyBlock(propBlock, hitMaterialIndex);

            elapsed += Time.deltaTime;
            yield return null;
        }

        propBlock.SetColor("_BaseColor", noAlphaColor);
        m_enemySkin.SetPropertyBlock(propBlock, hitMaterialIndex);
    }
}
