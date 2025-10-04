using UnityEngine;
using UnityEngine.InputSystem;

public class CannonController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 90f;
    
    [Header("Turret Settings")]
    public Transform turret; 
    public Transform barrel;
    public float turretRotationSpeed = 60f;
    public float barrelElevationSpeed = 30f;
    public float minBarrelAngle = -10f;
    public float maxBarrelAngle = 30f;
    
    private Rigidbody rb;
    private float currentBarrelAngle = 0f;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }
    
    
    void FixedUpdate()
    {
        TurretRotation();
        BarrelElevation();
        Movement();
        TankRotation();
    }
    
    void Movement()
    {
        float moveInput = 0f;
        
        if (Input.GetKey(KeyCode.W))
        {
            moveInput = 1f;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            moveInput = -1f;
        }
        
        Vector3 movement = transform.forward * moveInput * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + movement);
    }
    
    void TankRotation()
    {
        float rotationInput = 0f;
        
        if (Input.GetKey(KeyCode.A))
        {
            rotationInput = -1f;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            rotationInput = 1f;
        }
        
        float rotation = rotationInput * rotationSpeed * Time.fixedDeltaTime;
        Quaternion deltaRotation = Quaternion.Euler(0f, rotation, 0f);
        rb.MoveRotation(rb.rotation * deltaRotation);
    }
    
    void TurretRotation()
    {

        float turretInput = 0f;
        
        if (Input.GetKey(KeyCode.Z))
        {
            turretInput = -1f;
        }
        else if (Input.GetKey(KeyCode.X))
        {
            turretInput = 1f;
        }
        
        float turretRotation = turretInput * turretRotationSpeed * Time.deltaTime;
        turret.Rotate(0f, 0f, turretRotation, Space.Self);
    }
    
    void BarrelElevation()
    {

        float barrelInput = 0f;
        
        if (Input.GetKey(KeyCode.Q))
        {
            barrelInput = 1f;
        }
        else if (Input.GetKey(KeyCode.E))
        {
            barrelInput = -1f;
        }
        

        currentBarrelAngle += barrelInput * barrelElevationSpeed * Time.deltaTime;
        currentBarrelAngle = Mathf.Clamp(currentBarrelAngle, minBarrelAngle, maxBarrelAngle);
        
        Vector3 barrelRotation = barrel.localEulerAngles;
        barrelRotation.x = currentBarrelAngle;
        barrel.localEulerAngles = barrelRotation;
    }
}