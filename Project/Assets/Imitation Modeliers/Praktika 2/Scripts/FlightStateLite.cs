﻿using UnityEngine;

namespace Plane
{
    [RequireComponent(typeof(Rigidbody))]
    public class FlightStateLite : MonoBehaviour
    {
        private const float MinValueForAngleAttack = 1e-3f;
        
        [Header("References")]
        [SerializeField] private Transform _wingChord;

        // Основные параметры полета
        public float IAS { get; private set; }
        public float AoAdeg { get; private set; }
        public float Nz { get; private set; }
        public float Altitude { get; private set; }
        public float VerticalSpeed { get; private set; }

        // Дополнительные параметры
        public float MachNumber { get; private set; }
        public float TrueAirSpeed { get; private set; }

        private Rigidbody _rigidbody;
        private Vector3 _vPrev;
        private float _tPrev;
        private float _prevAltitude;
        
        private bool _wasStalled = false;

        private void Awake() => Initialize();

        private void Initialize()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _vPrev = _rigidbody.linearVelocity;
            _tPrev = Time.time;
            _prevAltitude = transform.position.y;

            if (_wingChord == null)
            {
                _wingChord = transform;
            }
        }

        private void FixedUpdate()
        {
            CalculateFlightParameters();
            CheckStallState();
        }

        private void CalculateFlightParameters()
        {
            Vector3 currentVelocity = _rigidbody.linearVelocity;
            
            // Скорость
            IAS = currentVelocity.magnitude;
            TrueAirSpeed = IAS;
            
            // Число Маха
            MachNumber = IAS / 340f;

            // Угол атаки
            if (IAS > MinValueForAngleAttack)
            {
                Vector3 flow = (-currentVelocity).normalized;
                float flowX = Vector3.Dot(flow, _wingChord.forward);
                float flowZ = Vector3.Dot(flow, _wingChord.up);
                AoAdeg = Mathf.Atan2(flowZ, flowX) * Mathf.Rad2Deg;
            }
            else
            {
                AoAdeg = 0;
            }

            // Перегрузка (исправленная формула)
            float currentTime = Time.time;
            float dt = Mathf.Max(MinValueForAngleAttack, currentTime - _tPrev);
            Vector3 aWorld = (currentVelocity - _vPrev) / dt;
            
            // Учитываем только ускорение от сил, без гравитации
            float aVert = Vector3.Dot(aWorld, transform.up);
            Nz = 1f + (aVert / Mathf.Abs(Physics.gravity.y));

            // Высота и скороподъемность
            Altitude = transform.position.y;
            VerticalSpeed = (Altitude - _prevAltitude) / dt;

            // Сохранение состояния для следующего кадра
            _vPrev = currentVelocity;
            _tPrev = currentTime;
            _prevAltitude = Altitude;
        }

        private void CheckStallState()
        {
            bool isStalled = Mathf.Abs(AoAdeg) > 16f && IAS < 30f;
            
            if (isStalled != _wasStalled)
            {
                _wasStalled = isStalled;
            }
        }

        private void OnGUI()
        {
            GUI.color = Color.black;
            GUILayout.BeginArea(new Rect(12, 460, 300, 130), GUI.skin.box);
            GUI.color = Color.white;
            GUILayout.Label("Flight State");
            GUILayout.Label($"IAS: {IAS:0.0} m/s | TAS: {TrueAirSpeed:0.0} m/s");
            GUILayout.Label($"Mach: {MachNumber:0.00} | Alt: {Altitude:0} m");
            GUILayout.Label($"V/S: {VerticalSpeed:0.0} m/s | AoA: {AoAdeg:0.0}°");
            GUILayout.Label($"G-Load: {Nz:0.0}g | Stall: {(_wasStalled ? "YES" : "NO")}");
            GUILayout.EndArea();
        }

        public bool IsOnGround()
        {
            return Altitude < 2f && IAS < 5f;
        }

        public bool IsCriticalAoA()
        {
            return Mathf.Abs(AoAdeg) > 20f;
        }

        public float GetSpeedPercent()
        {
            return Mathf.Clamp01(IAS / 100f);
        }
    }
}