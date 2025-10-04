using UnityEngine;

public class Target : MonoBehaviour
{
    [Header("Physics Settings")]
    public bool randomGravity = true;
    public float minMass = 0.5f;
    public float maxMass = 3f;
    public float minScale = 0.5f;
    public float maxScale = 2f;
    public float minHorizontalForce = -5f;
    public float maxHorizontalForce = 5f;
    
    private Rigidbody rb;
    private TargetManager targetManager;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        targetManager = FindObjectOfType<TargetManager>();
        
        InitializeTarget();
    }
    
    void InitializeTarget()
    {

        
        rb.mass = Random.Range(minMass, maxMass);
        
        float randomScale = Random.Range(minScale, maxScale);
        transform.localScale = Vector3.one * randomScale;
        
        float randomForceX = Random.Range(minHorizontalForce, maxHorizontalForce);
        float randomForceZ = Random.Range(minHorizontalForce, maxHorizontalForce);
        Vector3 randomForce = new Vector3(randomForceX, 0f, randomForceZ);
        
        rb.AddForce(randomForce, ForceMode.VelocityChange);
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Bullet"))
        {
            Destroy(other.gameObject);
            
            if (targetManager != null)
            {
                targetManager.AddScore(1);
            }
            
            Destroy(gameObject);
        }
    }
}