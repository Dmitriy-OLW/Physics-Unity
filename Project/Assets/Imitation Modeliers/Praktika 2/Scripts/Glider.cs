using System;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Glider : MonoBehaviour
{
    [SerializeField] private Transform _wingCP;
    [Header("Плотность воздуха")]
    [SerializeField] private float _airDensity = 1.225f;
        
    [Header("Аэродинамические характеристики крыла")]
    [SerializeField] private float _wingArea = 1.5f;
    [SerializeField] private float _wingAspect = 8.0f;
    [SerializeField] private float _wingCDO = 0.02f;
    [SerializeField] private float _wingCLaplha = 5.5f;
    
    
    private Vector3 _vPoint;
    
    private Rigidbody _rb;

    private Vector3 _worldVelocity;
    private float _speedMS;
    private float _alphaRad;

    private float CL, CD, gDyn, Lmag, Dmag, glideK;
    

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        
    }

    private void FixedUpdate()
    {
        _vPoint = _rb.GetPointVelocity(_wingCP.position);
        _speedMS = _vPoint.magnitude;

        Vector3 flowDir = (-_vPoint).normalized;
        Vector3 xClord = _wingCP.forward;
        Vector3 zUP = _wingCP.up;
        Vector3 ySpan = _wingCP.right;
        
        float flowX = Vector3.Dot(flowDir, xClord);
        float flowZ = Vector3.Dot(flowDir, zUP);
        
        _alphaRad = MathF.Atan2(flowZ, flowX);


        CL = _wingCLaplha * _alphaRad;
        CD = _wingCDO * CL * CL / (Mathf.PI * _wingAspect * 0.85f);

        gDyn = 0.5f * _airDensity * _speedMS * _speedMS;
        Lmag = gDyn * _wingArea * CL;
        Dmag = gDyn * _wingArea * CD;

        Vector3 Ddir = -flowDir;
        
        Vector3 liftDir = Vector3.Cross(flowDir, ySpan);
        
        liftDir.Normalize();

        Vector3 L = Lmag * liftDir;
        Vector3 D = Dmag * Ddir;
        
        _rb.AddForceAtPosition(L+D, _wingCP.position, ForceMode.Force);


    }


    private void StepOne()
    {
        
        
        
        _worldVelocity = _rb.linearVelocity;
        _speedMS = _worldVelocity.magnitude;

        Vector3 xClord = _wingCP.forward;
        Vector3 zUP = _wingCP.up;

        Vector3 flowDir = _speedMS > 0 ? -_worldVelocity.normalized : _wingCP.forward;

        float flowX = Vector3.Dot(flowDir, xClord);
        float flowZ = Vector3.Dot(flowDir, zUP);


        _alphaRad = MathF.Atan2(flowZ, flowX);


    }

    private void OnGUI()
    {
        GUI.color = Color.black;

        GUILayout.Label($"Speed: {_speedMS:0.0} m/s");
        GUILayout.Label($"Angle atack: {_alphaRad * Mathf.Deg2Rad:0.0} m/s");

    }
}

