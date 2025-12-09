using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Renderer))]
public class LiquidMaterialHelper : MonoBehaviour
{
    private Renderer rend;
    private MaterialPropertyBlock propertyBlock;
    
    [Header("Lighting Fix")]
    [Tooltip("Ajuda a corrigir a iluminação da backface")]
    public bool UseBackfaceLightingFix = true;
    
    [Range(0f, 1f)]
    public float BackfaceIntensity = 0.7f;
    
    private void OnEnable()
    {
        rend = GetComponent<Renderer>();
        propertyBlock = new MaterialPropertyBlock();
        UpdateMaterialProperties();
    }
    
    private void Update()
    {
        if (Application.isEditor && !Application.isPlaying)
        {
            UpdateMaterialProperties();
        }
    }
    
    private void UpdateMaterialProperties()
    {
        if (rend == null || rend.sharedMaterial == null)
            return;
            
        if (UseBackfaceLightingFix)
        {
            rend.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat("_BackfaceIntensity", BackfaceIntensity);
            rend.SetPropertyBlock(propertyBlock);
        }
    }
}
