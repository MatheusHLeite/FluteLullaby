using UnityEngine;

public class Wobble : MonoBehaviour
{
    private Renderer rend;
    private Vector3 lastPos;
    private Vector3 velocity;
    private Vector3 lastRot;  
    private Vector3 angularVelocity;
    
    [Header("Wobble Settings")]
    public float MaxWobble = 0.03f;
    public float WobbleSpeed = 1f;
    public float Recovery = 1f;
    
    [Header("Damping Settings")]
    public float Damping = 0.95f;
    public float VelocityMultiplier = 1.5f;
    public float AngularVelocityMultiplier = 0.3f;

    [Header("Wave Settings")]
    public float WaveIntensityMax = 1f;
    public float WaveRecovery = 2f;

    private float currentWaveIntensity;

    private float wobbleAmountX;
    private float wobbleAmountZ;
    private float wobbleAmountToAddX;
    private float wobbleAmountToAddZ;
    private float velocityWobbleX;
    private float velocityWobbleZ;
    private float pulse;
    private float time = 0.5f;
    
    private const float SMOOTHING_FACTOR = 5f;
    
    void Start()
    {
        rend = GetComponent<Renderer>();
        lastPos = transform.position;
        lastRot = transform.rotation.eulerAngles;
    }
    
    private void Update()
    {
        time += Time.deltaTime;
        
        velocity = (lastPos - transform.position) / Time.deltaTime;
        angularVelocity = NormalizeAngles(transform.rotation.eulerAngles - lastRot);
        
        float inputX = (velocity.x * VelocityMultiplier + angularVelocity.z * AngularVelocityMultiplier);
        float inputZ = (velocity.z * VelocityMultiplier + angularVelocity.x * AngularVelocityMultiplier);
        
        wobbleAmountToAddX += inputX * MaxWobble * Time.deltaTime * SMOOTHING_FACTOR;
        wobbleAmountToAddZ += inputZ * MaxWobble * Time.deltaTime * SMOOTHING_FACTOR;
        
        wobbleAmountToAddX = Mathf.Clamp(wobbleAmountToAddX, -MaxWobble, MaxWobble);
        wobbleAmountToAddZ = Mathf.Clamp(wobbleAmountToAddZ, -MaxWobble, MaxWobble);
        
        pulse = 2f * Mathf.PI * WobbleSpeed;
        wobbleAmountX = wobbleAmountToAddX * Mathf.Sin(pulse * time);
        wobbleAmountZ = wobbleAmountToAddZ * Mathf.Sin(pulse * time);
        
        wobbleAmountX += velocityWobbleX * Mathf.Sin(pulse * time * 0.5f);
        wobbleAmountZ += velocityWobbleZ * Mathf.Sin(pulse * time * 0.5f);

        float speed = velocity.magnitude + angularVelocity.magnitude * 0.2f;
        float targetIntensity = Mathf.Clamp01(speed);

        currentWaveIntensity = Mathf.Lerp(currentWaveIntensity, targetIntensity, Time.deltaTime * WaveRecovery);

        rend.material.SetFloat("_WobbleX", wobbleAmountX);
        rend.material.SetFloat("_WobbleZ", wobbleAmountZ);
        rend.material.SetFloat("_WaveAmplitude", MaxWobble * currentWaveIntensity * WaveIntensityMax);

        wobbleAmountToAddX = Mathf.Lerp(wobbleAmountToAddX, 0, Time.deltaTime * Recovery);
        wobbleAmountToAddZ = Mathf.Lerp(wobbleAmountToAddZ, 0, Time.deltaTime * Recovery);
        
        velocityWobbleX *= Damping;
        velocityWobbleZ *= Damping;
        
        velocityWobbleX += inputX * MaxWobble * 0.1f;
        velocityWobbleZ += inputZ * MaxWobble * 0.1f;
        
        lastPos = transform.position;
        lastRot = transform.rotation.eulerAngles;
    }
    
    private Vector3 NormalizeAngles(Vector3 angles)
    {
        angles.x = NormalizeAngle(angles.x);
        angles.y = NormalizeAngle(angles.y);
        angles.z = NormalizeAngle(angles.z);
        return angles;
    }
    
    private float NormalizeAngle(float angle)
    {
        while (angle > 180f)
            angle -= 360f;
        while (angle < -180f)
            angle += 360f;
        return angle;
    }
}