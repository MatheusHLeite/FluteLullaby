using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class LiquidTextureController : MonoBehaviour
{
    private Renderer rend;
    private Material materialInstance;
    
    [Header("Transparency Settings")]
    [Range(0f, 1f)]
    [Tooltip("Transparência do líquido (0 = transparente, 1 = opaco)")]
    public float Alpha = 0.6f;
    
    [Header("Texture Settings")]
    [Tooltip("Textura opcional para o líquido")]
    public Texture2D LiquidTexture;
    
    [Tooltip("Usar textura ao invés de cor sólida")]
    public bool UseTexture = false;
    
    [Header("Texture Animation")]
    [Tooltip("Velocidade de movimento da textura (X e Z)")]
    public Vector2 TextureSpeed = new Vector2(0f, 0.05f);
    
    [Tooltip("Escala do tiling da textura")]
    public Vector2 TextureTiling = new Vector2(1f, 1f);
    
    [Tooltip("Movimento de textura segue o wobble")]
    public bool SyncWithWobble = true;
    
    [Range(0f, 1f)]
    [Tooltip("Intensidade do movimento da textura com wobble")]
    public float WobbleTextureIntensity = 0.3f;
    
    [Header("Advanced")]
    [Tooltip("Rotação da textura ao longo do tempo")]
    public float TextureRotationSpeed = 0f;
    
    private Vector2 currentTextureOffset;
    private Wobble wobbleScript;
    
    private static readonly int AlphaPropertyID = Shader.PropertyToID("_Alpha");
    private static readonly int MainTexPropertyID = Shader.PropertyToID("_MainTexture");
    private static readonly int TextureTilingPropertyID = Shader.PropertyToID("_TextureTiling");
    private static readonly int TextureOffsetPropertyID = Shader.PropertyToID("_TextureOffset");
    private static readonly int UseTexturePropertyID = Shader.PropertyToID("_UseTexture");
    private static readonly int TextureRotationPropertyID = Shader.PropertyToID("_TextureRotation");
    
    void Start()
    {
        rend = GetComponent<Renderer>();
        wobbleScript = GetComponent<Wobble>();
        
        materialInstance = rend.material;
        
        InitializeMaterial();
    }
    
    void InitializeMaterial()
    {
        if (materialInstance == null) return;
        
        if (materialInstance.HasProperty(AlphaPropertyID))
            materialInstance.SetFloat(AlphaPropertyID, Alpha);
            
        if (materialInstance.HasProperty(MainTexPropertyID) && LiquidTexture != null)
            materialInstance.SetTexture(MainTexPropertyID, LiquidTexture);
            
        if (materialInstance.HasProperty(UseTexturePropertyID))
            materialInstance.SetFloat(UseTexturePropertyID, UseTexture ? 1f : 0f);
            
        if (materialInstance.HasProperty(TextureTilingPropertyID))
            materialInstance.SetVector(TextureTilingPropertyID, new Vector4(TextureTiling.x, TextureTiling.y, 0, 0));
    }
    
    void Update()
    {
        if (materialInstance == null) return;
        
        UpdateAlpha();
        UpdateTexture();
        UpdateTextureAnimation();
    }
    
    void UpdateAlpha()
    {
        if (materialInstance.HasProperty(AlphaPropertyID))
        {
            materialInstance.SetFloat(AlphaPropertyID, Alpha);
        }
    }
    
    void UpdateTexture()
    {
        if (materialInstance.HasProperty(MainTexPropertyID) && LiquidTexture != null)
        {
            materialInstance.SetTexture(MainTexPropertyID, LiquidTexture);
        }
        
        if (materialInstance.HasProperty(UseTexturePropertyID))
        {
            materialInstance.SetFloat(UseTexturePropertyID, UseTexture ? 1f : 0f);
        }
        
        if (materialInstance.HasProperty(TextureTilingPropertyID))
        {
            materialInstance.SetVector(TextureTilingPropertyID, new Vector4(TextureTiling.x, TextureTiling.y, 0, 0));
        }
    }
    
    void UpdateTextureAnimation()
    {
        currentTextureOffset += TextureSpeed * Time.deltaTime;
        
        if (SyncWithWobble && wobbleScript != null)
        {
            float wobbleInfluenceX = 0f;
            float wobbleInfluenceZ = 0f;
            
            if (materialInstance.HasProperty("_WobbleX"))
                wobbleInfluenceX = materialInstance.GetFloat("_WobbleX");
            if (materialInstance.HasProperty("_WobbleZ"))
                wobbleInfluenceZ = materialInstance.GetFloat("_WobbleZ");
            
            currentTextureOffset.x += wobbleInfluenceX * WobbleTextureIntensity * Time.deltaTime;
            currentTextureOffset.y += wobbleInfluenceZ * WobbleTextureIntensity * Time.deltaTime;
        }
        
        if (materialInstance.HasProperty(TextureOffsetPropertyID))
        {
            materialInstance.SetVector(TextureOffsetPropertyID, new Vector4(currentTextureOffset.x, currentTextureOffset.y, 0, 0));
        }
        
        if (TextureRotationSpeed != 0f && materialInstance.HasProperty(TextureRotationPropertyID))
        {
            float rotation = Time.time * TextureRotationSpeed;
            materialInstance.SetFloat(TextureRotationPropertyID, rotation);
        }
    }
    
    void OnDestroy()
    {
        if (materialInstance != null)
        {
            Destroy(materialInstance);
        }
    }
}
