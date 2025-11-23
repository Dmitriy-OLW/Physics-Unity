﻿using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Plane
{
    [RequireComponent(typeof(Rigidbody))]
    public class FlightController : MonoBehaviour
    {
        [SerializeField] private InputActionAsset _playerInput;
        [SerializeField] private FlightProtection _flightProtection;

        [Header("Rate Control (PD)")] 
        [SerializeField] private Vector3 _maxRateDeg = new Vector3(90, 90, 120);
        [SerializeField] private Vector3 _kp = new Vector3(3, 2, 3);
        [SerializeField] private Vector3 _kd = new Vector3(0.8f, 0.6f, 0.9f);
        [SerializeField] private Vector3 _maxTorque = new Vector3(30, 25, 35);

        [Header("Стабилизация")]
        [SerializeField] private Vector2 _attHoldKp = new Vector2(2, 2);
        [SerializeField] private float _attHoldMaxRate = 45f;

        [Header("Демпфирование")]
        [SerializeField] private Vector3 _naturalDamping = new Vector3(2, 1, 2);

        private Rigidbody _rigidbody;
        private InputAction _yaw, _pitch, _roll;
        private InputAction _hold;

        private float _targetPitchDeg, _targetRollDeg;
        private bool _isHolding;
        private Vector3 _omegaBodyDeg;

        private void Awake() => Initialize();

        private void Initialize()
        {
            _rigidbody = GetComponent<Rigidbody>();
            
            var map = _playerInput.FindActionMap("Player");

            _pitch = map.FindAction("Pitch");
            _roll = map.FindAction("Roll");
            _yaw = map.FindAction("Yaw");
            _hold = map.FindAction("HoldAttribute");
            
            _playerInput.Enable();
        }

        private void OnEnable()
        {
            _hold.performed += OnHoldOn;
            _hold.canceled += OnHoldOff;
        }

        private void OnDisable()
        {
            _hold.performed -= OnHoldOn;
            _hold.canceled -= OnHoldOff;
        }

        private void OnHoldOn(InputAction.CallbackContext _)
        {
            _isHolding = true;
            var e = GetLocalPitchRollDeg();
            _targetPitchDeg = e.xPitch;
            _targetRollDeg = e.zRoll;
        }

        private void OnHoldOff(InputAction.CallbackContext _) => _isHolding = false;

        private (float xPitch, float zRoll) GetLocalPitchRollDeg()
        {
            Vector3 e = transform.localEulerAngles;
            float pitch = NormalizeAngle(e.x);
            float roll = NormalizeAngle(e.z);
            return (pitch, roll);
        }

        private float NormalizeAngle(float angle) => (angle > 180) ? angle - 360 : angle;

        private void FixedUpdate()
        {
            Vector3 omega = _rigidbody.angularVelocity;
            Vector3 omegaBody = transform.InverseTransformDirection(omega);
            _omegaBodyDeg = omegaBody * Mathf.Rad2Deg;

            // Угловое управление
            Vector3 rateCmdDeg = ReadRateCommandDeg();
            if (_isHolding)
                rateCmdDeg = GenerateHoldRateDeg();

            rateCmdDeg = _flightProtection.ApplyLimiters(rateCmdDeg);
            ApplyAngularControl(rateCmdDeg);
            ApplyNaturalDamping();
        }

        private void ApplyAngularControl(Vector3 rateCmdDeg)
        {
            Vector3 errDeg = rateCmdDeg - _omegaBodyDeg;
            
            Vector3 tau = new Vector3(
                _kp.x * errDeg.x + _kd.x * (rateCmdDeg.x - _omegaBodyDeg.x),
                _kp.y * errDeg.y + _kd.y * (rateCmdDeg.y - _omegaBodyDeg.y),
                _kp.z * errDeg.z + _kd.z * (rateCmdDeg.z - _omegaBodyDeg.z)
            );

            // Раздельное ограничение для каждой оси
            tau.x = Mathf.Clamp(tau.x, -_maxTorque.x, _maxTorque.x);
            tau.y = Mathf.Clamp(tau.y, -_maxTorque.y, _maxTorque.y);
            tau.z = Mathf.Clamp(tau.z, -_maxTorque.z, _maxTorque.z);
            
            _rigidbody.AddRelativeTorque(tau, ForceMode.Force);
        }

        private void ApplyNaturalDamping()
        {
            Vector3 dampingTorque = -Vector3.Scale(_omegaBodyDeg, _naturalDamping);
            _rigidbody.AddRelativeTorque(dampingTorque, ForceMode.Force);
        }

        private Vector3 GenerateHoldRateDeg()
        {
            var e = GetLocalPitchRollDeg();
            float errPitch = Mathf.DeltaAngle(e.xPitch, _targetPitchDeg);
            float errRoll = Mathf.DeltaAngle(e.zRoll, _targetRollDeg);

            float wPitch = Mathf.Clamp(errPitch * _attHoldKp.x, -_attHoldMaxRate, _attHoldMaxRate);
            float wRoll = Mathf.Clamp(errRoll * _attHoldKp.y, -_attHoldMaxRate, _attHoldMaxRate);
            
            return new Vector3(wPitch, 0, wRoll);
        }

        private Vector3 ReadRateCommandDeg()
        {
            float uPitch = _pitch.ReadValue<float>();
            float uRoll = _roll.ReadValue<float>();
            float uYaw = _yaw.ReadValue<float>();

            return new Vector3(uPitch * _maxRateDeg.x, uYaw * _maxRateDeg.y, uRoll * _maxRateDeg.z);
        }

        private void OnGUI()
        {
            GUI.color = Color.black;
            GUILayout.BeginArea(new Rect(12, 250, 300, 200), GUI.skin.box);
            GUI.color = Color.white;
            GUILayout.Label("Flight Controller");
            GUILayout.Label($"Скорость вращения: {_omegaBodyDeg.x:0}/{_omegaBodyDeg.y:0}/{_omegaBodyDeg.z:0}");
            GUILayout.Label($"Скорость полёта: {_rigidbody.linearVelocity.magnitude:0.0} m/s");
            GUILayout.Label($"Высота: {transform.position.y:0.0} m");
            GUILayout.Label($"AoA Warn: {_flightProtection.AoaWarn}");
            GUILayout.Label($"G Warn: {_flightProtection.GWarn}");
            GUILayout.Label($"Stall: {_flightProtection.Stall}");
            GUILayout.EndArea();
        }
    }
}