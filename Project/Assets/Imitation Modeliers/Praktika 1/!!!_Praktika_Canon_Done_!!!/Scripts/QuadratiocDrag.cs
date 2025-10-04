using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class QuadratiocDrag : MonoBehaviour
{
    private float _mass;
    private float _radius;
    private float _dragCoefficient;
    private float _airDensity;
    private Vector3 _wind = Vector3.zero;

    [SerializeField] Rigidbody _rb;
    private float _area;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        Vector3 vReal = _rb.linearVelocity - _wind;
        float speed = vReal.magnitude;

        if (speed < 1e-6f) return;

        Vector3 drag = -0.5f * _airDensity * _dragCoefficient * _area * speed * vReal;
        _rb.AddForce(drag, ForceMode.Force);
    }

    public void SetPhysicleParametrs(float mass, float radius, float dragCoefficient, float airDensity, Vector3 wind, Vector3 initialVelocity)
    {
        _mass = mass;
        _radius = radius;
        _dragCoefficient = dragCoefficient;
        _airDensity = airDensity;
        _wind = wind;

        _rb.mass = _mass;
        _rb.useGravity = true;
        _rb.linearVelocity = initialVelocity;

        _area = _radius * _radius * Mathf.PI;
    }
}