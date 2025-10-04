using System;
using Unity.Android.Gradle.Manifest;
using Unity.VisualScripting;
using UnityEngine;

namespace IMP{

    [RequireComponent(typeof(ForceVisualizers))]
    public class SimplyPhysicsEngine : MonoBehaviour
    {
        [Header("Physics Parametsr")] 
        [SerializeField]
        private float _mass = 1;
        [SerializeField]
        private float _linearDrag = 0.1f ;
        [SerializeField]
        private float _angularDrag = 0.05f;
        [SerializeField]
        private bool _isGravity = true;

        [SerializeField] private Vector3 _windForce;
        
        private Vector3 _velocity = Vector3.zero;
        
        
        private ForceVisualizers _forceVisualizers;
        private Vector3 _netForce;
        private void Start()
        {
            _forceVisualizers = GetComponent<ForceVisualizers>();
        }

        private void FixedUpdate()
        {
            UpdateWindForce();
            
            _netForce = Vector3.zero;
            _forceVisualizers.ClearForces();

            if (_isGravity)
            {
                Vector3 gravity = Physics.gravity * _mass;
                ApplyForce(gravity, Color.cyan, "Gravity");
            }
            ApplyForce(_windForce, Color.blue, "Wind");

            Vector3 acceleration = _netForce / _mass;
            IntrgateMotion(acceleration);
            _forceVisualizers.AddForce(_netForce, Color.red, "ForceMain");
        }

        private void IntrgateMotion(Vector3 acceleration)
        {
            _velocity += acceleration * Time.fixedDeltaTime;
            transform.position += _velocity * Time.fixedDeltaTime;
        }

        private void ApplyForce(Vector3 force, Color colorForce, string name)
        {
            _netForce += force;
            _forceVisualizers.AddForce(force, colorForce, name);
        }
        
        
        
        
        
        
        
        
        
        
        
        
        
        [Header("Wind Force Settings")]
        [SerializeField] 
        private float _windChangeSpeed = 1f;
        [SerializeField] 
        private float _windMagnitude = 5f;
        [SerializeField] 
        private Vector3 _windDirectionAxis = Vector3.up;
        
        private float _windTime;
        
        private void UpdateWindForce()
        {
            // Увеличиваем время для анимации
            _windTime += Time.fixedDeltaTime * _windChangeSpeed;
            
            // Вычисляем угол поворота на основе времени
            float angle = Mathf.Sin(_windTime) * 180f; // Плавное изменение угла от -180 до 180
            
            // Создаем вращение вокруг выбранной оси
            Quaternion rotation = Quaternion.AngleAxis(angle, _windDirectionAxis.normalized);
            
            // Вычисляем изменяющуюся величину силы (от 0.5 до 1.0 от максимальной)
            float magnitudeVariation = 0.5f + 0.5f * Mathf.Sin(_windTime * 0.7f);
            float currentMagnitude = _windMagnitude * magnitudeVariation;
            
            // Применяем вращение к начальному направлению (можно использовать Vector3.forward как базовое)
            Vector3 baseDirection = Vector3.forward;
            Vector3 rotatedDirection = rotation * baseDirection;
            
            // Устанавливаем новую силу ветра
            _windForce = rotatedDirection * currentMagnitude;
        }
    }
    
}

