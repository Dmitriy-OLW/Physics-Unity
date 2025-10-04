using System;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class TraectoriRender : MonoBehaviour
{
    [Header("Trajectory parameters")] 
    [SerializeField]
    private float _lineWidth = 0.15f;

    [SerializeField] private int _pointCount = 30;
    [SerializeField] private float _timeStep = 0.1f;
    private LineRenderer _lineRender;

    private void Awake()
    {
        IninitializeLineRender();
    }

    private void IninitializeLineRender()
    {
        _lineRender = GetComponent<LineRenderer>();
        _lineRender.startWidth = _lineWidth;
        _lineRender.useWorldSpace = true;
        _lineRender.material = new Material(Shader.Find("Sprites/Default"));
    }

    public void DrawVacum(Vector3 startPosition, Vector3 startVelosity)
    {
        if (_pointCount < 2) _pointCount = 2;

        _lineRender.positionCount = _pointCount;
        
        for (int i = 0; i < _pointCount; i++)
        {
            float t = i * _timeStep;
            Vector3 newPosition = startPosition + t * startVelosity + Physics.gravity * t * t / 2;
            _lineRender.SetPosition(i, newPosition);
        }
    }

    public void DrawWithAirEuler(Vector3 startPosition, Vector3 startVelocity, float mass, float radius, float dragCoefficient, float airDensity, Vector3 wind)
    {
        if (_pointCount < 2) _pointCount = 2;

        _lineRender.positionCount = _pointCount;
        
        Vector3 p = startPosition;
        Vector3 v = startVelocity;
        float area = radius * radius * Mathf.PI;

        for (int i = 0; i < _pointCount; i++)
        {
            _lineRender.SetPosition(i, p);

            Vector3 vRel = v - wind;
            float speed = vRel.magnitude;
            Vector3 drag = Vector3.zero;
            
            if (speed > 1e-6f)
            {
                drag = -0.5f * airDensity * dragCoefficient * area * speed * vRel;
            }
            
            Vector3 a = Physics.gravity + drag / mass;
            v += a * _timeStep;
            p += v * _timeStep;
            
            //проверка для того чтобы обрезать лайн рендер при столкновение с препядствием
            if (Physics.SphereCast(p - v * _timeStep, radius, v.normalized, out RaycastHit hit, v.magnitude * _timeStep))
            {
                _lineRender.positionCount = i + 1;
                _lineRender.SetPosition(i, hit.point);
                break;
            }
        }
    }
}