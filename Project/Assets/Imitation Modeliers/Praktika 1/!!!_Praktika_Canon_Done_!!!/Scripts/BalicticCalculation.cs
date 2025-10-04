using System;
using System.Numerics;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.PlayerLoop;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

public class BalicticCalculation : MonoBehaviour
{
    [SerializeField] private Transform _launcPoint;
    [SerializeField] private float _muzzleVelocity = 20;
    [SerializeField, Range(0, 85)] private float _muzzleAngle = 20;

    private TraectoriRender _traectoriRender;
    [Space] [SerializeField] private QuadratiocDrag _shotRound;
    
    private float _mass = 1;
    private float _radius = 0.1f;
    private float _dragCoefficient = 0.47f;
    private float _airDensity = 1.225f;
    [SerializeField] private Vector3 _wind = Vector3.zero;
    
    [SerializeField] private float _minMass = 0.5f;
    [SerializeField] private float _maxMass = 2.0f;
    [SerializeField] private float _minRadius = 0.05f;
    [SerializeField] private float _maxRadius = 0.2f;
    
    private float _targetMass;
    private float _targetRadius;
    private float _massVelocity;
    private float _radiusVelocity;
    [SerializeField] private float _smoothTime = 1.0f;
    
    private void Start()
    {
        _traectoriRender = GetComponent<TraectoriRender>();
    }

    private void Update()
    {
        if(_launcPoint == null) return;
        
            //GenerateRandomParameters();
        
        _mass = Mathf.SmoothDamp(_mass, _targetMass, ref _massVelocity, _smoothTime);
        _radius = Mathf.SmoothDamp(_radius, _targetRadius, ref _radiusVelocity, _smoothTime);
        
        // Проверка достижения целевых значений
        if (Mathf.Abs(_mass - _targetMass) < 0.01f && Mathf.Abs(_radius - _targetRadius) < 0.001f)
        {
            GenerateNewTargetParameters();
        }
        
        
        Vector3 v0 = CalculateVelocityVector(_muzzleAngle);
        _traectoriRender.DrawWithAirEuler(
            _launcPoint.position, 
            v0, 
            _mass, 
            _radius, 
            _dragCoefficient, 
            _airDensity, 
            _wind
        );

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        { 
            Fire(v0);
        }
    }
    private void GenerateNewTargetParameters()
    {
        _targetMass = UnityEngine.Random.Range(_minMass, _maxMass);
        _targetRadius = UnityEngine.Random.Range(_minRadius, _maxRadius);
    }
    
    private void GenerateRandomParameters()
    {
        _mass = UnityEngine.Random.Range(_minMass, _maxMass);
        _radius = UnityEngine.Random.Range(_minRadius, _maxRadius);
    }

    private void Fire(Vector3 initialVelocity)
    {
        if (_shotRound != null)
        {
            GameObject newShootRound = Instantiate(_shotRound.gameObject, _launcPoint.position, Quaternion.identity);
            QuadratiocDrag quadratiocDrag = newShootRound.GetComponent<QuadratiocDrag>();
            quadratiocDrag.SetPhysicleParametrs(_mass, _radius, _dragCoefficient, _airDensity, _wind, initialVelocity);
        }
    }

    private Vector3 CalculateVelocityVector(float angle)
    {
        float vx = _muzzleVelocity * Mathf.Cos(angle * Mathf.Deg2Rad);
        float vy = _muzzleVelocity * Mathf.Sin(angle * Mathf.Deg2Rad);
        return _launcPoint.forward * vx + _launcPoint.up * vy;
    }
}