﻿using UnityEngine;

namespace Plane
{
    [RequireComponent(typeof(Rigidbody))]
    public class FlightProtection : MonoBehaviour
    {
        [Header("References")] 
        [SerializeField] private FlightController _flightController;
        [SerializeField] private FlightStateLite _flightState;

        [Header("AoA Limiters")]
        [SerializeField] private float _aoaSoft = 14f;
        [SerializeField] private float _aoaHard = 18f;

        [Header("G Limiters")] 
        [SerializeField] private float _gPos = 9;
        [SerializeField] private float _gNeg = -3f;
        [SerializeField] private float _gBlend = 1f;

        [Header("Срыв")]
        [SerializeField] private float _stallAoa = 17;
        [SerializeField] private float _stallFade = 3;

        [Header("Турбулентность")] 
        [SerializeField] private bool _useTurb = false;
        [SerializeField] private float _turbTorque = 8;
        [SerializeField] private float _turbForce = 150;
        [SerializeField] private float _turbFilter = 2;

        private Rigidbody _rigidbody;
        private Vector3 _turboTorqueState, _turboForceState;

        public bool AoaWarn { get; private set; }
        public bool GWarn { get; private set; }
        public bool Stall { get; private set; }
        public float CurrentAoA => _flightState != null ? _flightState.AoAdeg : 0f;
        public float CurrentG => _flightState != null ? _flightState.Nz : 1f;

        private void Awake() 
        { 
            _rigidbody = GetComponent<Rigidbody>();
            
            if (_flightController == null)
                _flightController = GetComponent<FlightController>();
            if (_flightState == null)
                _flightState = GetComponent<FlightStateLite>();
        }

        private float SoftGate(float soft, float hard, float value)
        {
            if (hard <= soft) return 0;
            if (value <= soft) return 1;
            if (value >= hard) return 0;
            float t = (value - soft) / (hard - soft);
            return 1 - (t * t * (3 - 2 * t));
        }

        public Vector3 ApplyLimiters(Vector3 cmdRateDeg)
        {
            if (_flightState == null) return cmdRateDeg;

            float aoa = Mathf.Abs(_flightState.AoAdeg);
            float nz = _flightState.Nz;

            // 1) AoA limiter
            float kAoa = SoftGate(_aoaSoft, _aoaHard, aoa);
            AoaWarn = aoa > _aoaSoft;
            cmdRateDeg.x *= kAoa;

            // 2) G-limiter
            float kG = 1f;
            if (nz > _gPos)
                kG = SoftGate(_gPos, _gPos + _gBlend, nz);
            else if (nz < _gNeg)
                kG = SoftGate(-_gNeg, -(_gNeg + _gBlend), -nz);

            GWarn = (nz > _gPos * 0.95f) || (nz < _gNeg * 0.95f);
            cmdRateDeg.x *= kG;

            // 3) Предупреждение о срыве
            Stall = aoa > _stallAoa;

            // 4) Дополнительное уменьшение управления при срыве
            if (Stall)
            {
                float stallFactor = Mathf.Clamp01((aoa - _stallAoa) / _stallFade);
                cmdRateDeg.x *= (1f - stallFactor * 0.8f);
                cmdRateDeg.z *= (1f - stallFactor * 0.6f);
            }

            return cmdRateDeg;
        }

        private void FixedUpdate()
        {
            if (_useTurb)
            {
                ApplyTurbulence();
            }
        }

        private void ApplyTurbulence()
        {
            _turboTorqueState = LowPass(
                _turboTorqueState,
                Random.insideUnitSphere * _turbTorque,
                _turbFilter
            );

            _turboForceState = LowPass(
                _turboForceState,
                Random.insideUnitSphere * _turbForce,
                _turbFilter
            );

            _rigidbody.AddRelativeTorque(_turboTorqueState, ForceMode.Force);
            _rigidbody.AddForce(_turboForceState, ForceMode.Force);
        }

        private Vector3 LowPass(Vector3 state, Vector3 target, float tau)
        {
            float dt = Time.fixedDeltaTime;
            float a = Mathf.Clamp01(dt / (tau + 1e-3f));
            return Vector3.Lerp(state, target, a);
        }

        private void OnGUI()
        {
            GUI.color = Color.black;
            GUILayout.BeginArea(new Rect(12, 600, 300, 100), GUI.skin.box);
            GUI.color = Color.white;
            GUILayout.Label("Flight Protection");
            GUILayout.Label($"AoA: {CurrentAoA:0.0}° {(AoaWarn ? "WARN!" : "")}");
            GUILayout.Label($"G-Load: {CurrentG:0.0}g {(GWarn ? "WARN!" : "")}");
            GUILayout.Label($"Stall: {(Stall ? "YES!" : "No")}");
            GUILayout.EndArea();
        }
    }
}