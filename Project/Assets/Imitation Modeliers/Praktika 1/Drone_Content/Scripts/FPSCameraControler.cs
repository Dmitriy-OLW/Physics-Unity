using System;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class FPSCameraControler : MonoBehaviour
{
    [SerializeField] private float _yawSensity = 180f;
    [SerializeField] private float _pitchSensity = 180f;
    [SerializeField] private float _maxPitchDrag = 89f;

    [SerializeField, Range(0, 1)] private float _rotationDamping;
    
    private float _yawDeg;
    private float _pitchDeg;
    private Quaternion _targetRotation;

    private void Aweke()
    {
        _yawDeg = transform.eulerAngles.y;
        _pitchDeg = transform.eulerAngles.x;

        _targetRotation = transform.rotation;
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        float dx = Input.GetAxis("Mouse Y");
        float dy = Input.GetAxis("Mouse X");

        _yawDeg += dy * _yawSensity * Time.deltaTime;
        _pitchDeg -= dx * _pitchSensity * Time.deltaTime;

        _pitchDeg = Mathf.Clamp(_pitchDeg, -_maxPitchDrag, _maxPitchDrag);
        
        Quaternion yawRot = Quaternion.AngleAxis(_yawDeg, Vector3.up);

        Vector3 rifghtAxis = yawRot * Vector3.right;
        
        Quaternion pitchRot = Quaternion.AngleAxis(_pitchDeg, rifghtAxis);

        _targetRotation = pitchRot * yawRot;

        float t = 1 - Mathf.Pow(1 - Mathf.Clamp01(_rotationDamping), Time.deltaTime * 60f);
        
        transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotation, t);
        
        //transform.localRotation = Quaternion.Euler(_pitchDeg, _yawDeg, 0);
    }
}

