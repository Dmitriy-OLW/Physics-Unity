using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class QuadRotorPDControler : MonoBehaviour
{

    private Rigidbody _rb;

    [Header("PhysicalParametr")] 
    [SerializeField]
    private float _mass = 1.5f;

    [SerializeField] private float _maxThrottle = 30f;
    [SerializeField] private float _maxTorquw = 5f;

    [SerializeField] private float _maxPitchDeg = 20f;
    [SerializeField] private float _maxYawDeg = 20f;
    [SerializeField] private float _yawRateDegPerSec = 90f;
    
    [SerializeField] private float _maxRollhDeg = 20f;

    private float _desiredYawDeg;
    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.mass = Mathf.Max(0.01f, _mass);

        _desiredYawDeg = transform.eulerAngles.y;
    }

    private void Update()
    {
        float yawInput = Mathf.Clamp(Input.GetAxis("Mouse X"), -1, 1);
        _desiredYawDeg += yawInput * _yawRateDegPerSec * Time.deltaTime;
    }

    private void FixedUpdate()
    {
        float pitchInput = Mathf.Clamp(Input.GetAxis("Vertical"), -1f, 1f);
        float roolInput = Mathf.Clamp(Input.GetAxis("Horizontal"), -1f, 1f);

        

        float throttleInput = Keyboard.current.spaceKey.isPressed ? 1f : 0f;

        float targetPitch = pitchInput * _maxPitchDeg;
        float targetRoll = -roolInput * _maxRollhDeg;

        Quaternion qTarget = quaternion.Euler(targetPitch, targetRoll, targetRoll);
        Quaternion qCurrent = _rb.rotation;

        Quaternion qError = qTarget * Quaternion.Inverse(qCurrent);
        if (qError.w < 0)
        {
            qError.x = -qError.x;
            qError.y = -qError.y;
            qError.z = -qError.z;
            qError.w = -qError.w;
        }
        qError.ToAngleAxis(out float angleDeg, out Vector3 axis);

        float angleRed = Mathf.Deg2Rad * angleDeg;

        Vector3 omega = _rb.angularVelocity;
        Vector3 torque = 8 * angleRed * axis -2.5f * omega;
        
        _rb.AddTorque(torque);
        
        float g = Physics.gravity.magnitude;
        float hover = g * _rb.mass;

        float comanded = Mathf.Lerp(hover - 0.5f * _maxThrottle, hover * 0.5f * _maxThrottle, throttleInput);
        float totalThrottle = Mathf.Clamp(comanded, 0, _maxThrottle);

        
        
        _rb.AddForce(transform.up*totalThrottle, ForceMode.Force);


    }
}
