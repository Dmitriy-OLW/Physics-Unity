using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Car
{
    public class KartController : MonoBehaviour
    {
        [SerializeField] private InputActionAsset _playerInput;
        
        [Header("Wheels")] 
        [SerializeField] private Transform _frontLeftWheel;
        [SerializeField] private Transform _frontRightWheel;
        [SerializeField] private Transform _rearLeftWheel;
        [SerializeField] private Transform _rearRightWheel;

        [SerializeField, Range(0, 1)] private float _frontAxisShare = 0.5f;

        [Header("Engine")] 
        [SerializeField] private float _engineTorque = 400f;
        [SerializeField] private float _wheelRadius = 0.3f;
        [SerializeField] private float _maxSpeed = 20;

        [Header("Steering")] 
        [SerializeField] private float _maxSteeringAngle = 30f;

        private Quaternion _frontLeftInitialLocalRot;
        private Quaternion _frontRightInitialLocalRot;
        
        [Header("Tyre friction")] 
        [SerializeField] private float _frictionCoefficient = 1.0f;
        [SerializeField] private float _lateralStiffness = 80f;
        [SerializeField] private float _rollingResistance = 0.5f;
        
        [Header("Ground Detection")]
        [SerializeField] private LayerMask _groundLayerMask = 1;
        [SerializeField] private float _groundCheckDistance = 0.5f;
        [SerializeField] private Transform _groundCheckPoint; 
        
        private Rigidbody _rb;

        private InputAction _moveAction;
        private float _throttleInput;
        private float _steerInput;

        private float _frontLeftNormalForce;
        private float _frontRightNormalForce;
        private float _rearLeftNormalForce;
        private float _rearRightNormalForce;

        private bool _isGrounded;
        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            var map = _playerInput.FindActionMap("Kart");
            _moveAction = map.FindAction("Move");

            _frontLeftInitialLocalRot = _frontLeftWheel.localRotation;
            _frontRightInitialLocalRot = _frontRightWheel.localRotation;
        }

        private void Start()
        {
            ComputeStaticWheelLoad();
        }

        private void OnEnable()
        {
            _playerInput.Enable();
        }

        private void OnDisable()
        {
            _playerInput.Disable();
        }

        private void ReadInput()
        {
            Vector2 move = _moveAction.ReadValue<Vector2>();
            _steerInput = Mathf.Clamp(move.x, -1, 1);
            _throttleInput = Mathf.Clamp(move.y, -1, 1);
        }

        private void RotateFrontWheel()
        {
            float steerAngle = _maxSteeringAngle * _steerInput;
            Quaternion steerRotation = Quaternion.Euler(0, steerAngle, 0);

            _frontLeftWheel.localRotation = _frontLeftInitialLocalRot * steerRotation;
            _frontRightWheel.localRotation = _frontRightInitialLocalRot * steerRotation;
        }
        
        private void ComputeStaticWheelLoad()
        {
            float mass = _rb.mass;
            float totalWeight = mass * Mathf.Abs(Physics.gravity.y);

            float frontWeight = totalWeight * _frontAxisShare;
            float rearWeight = totalWeight * (1 - _frontAxisShare);
            
            _frontLeftNormalForce = frontWeight * 0.5f;
            _frontRightNormalForce = frontWeight * 0.5f;
            _rearLeftNormalForce = rearWeight * 0.5f;
            _rearRightNormalForce = rearWeight * 0.5f;
        }
        
        private void Update()
        {
            ReadInput();
            RotateFrontWheel();
            CheckGrounded();
        }

        private void FixedUpdate()
        {
            if (_isGrounded)
            {
                ApplyEngineForces();
                ApplyWheelForces();
            }
            

        }

        private void ApplyWheelForces()
        {
            ApplyWheelForce(_frontRightWheel, _frontRightNormalForce, true, false); 
            ApplyWheelForce(_frontLeftWheel, _frontLeftNormalForce, true, false);   
            ApplyWheelForce(_rearLeftWheel, _rearLeftNormalForce, false, true);   
            ApplyWheelForce(_rearRightWheel, _rearRightNormalForce, false, true);
        }

        private void ApplyWheelForce(Transform wheel, float normalForce, bool isSteer, bool isDriven)
        {
            Vector3 wheelPos = wheel.position;
            Vector3 wheelForward = wheel.forward;
            Vector3 wheelRight = wheel.right;

            Vector3 velocity = _rb.GetPointVelocity(wheelPos);

            float vLong = Vector3.Dot(velocity, wheelForward);
            float vLat = Vector3.Dot(velocity, wheelRight);

            float Fx = 0f;
            float Fy = 0f;


            if (isDriven)
            {
                Vector3 bodyForward = transform.forward;
                float speedAlongForward = Vector3.Dot(_rb.linearVelocity, bodyForward);

                if (!(_throttleInput > 0 && speedAlongForward > _maxSpeed))
                {
                    float driveTorque = _engineTorque * _throttleInput;
                    float driveForce = driveTorque / _wheelRadius;
                    Fx += driveForce;
                }
            }


            float rolling = -_rollingResistance * vLong;
            Fx += rolling;


            if (isSteer)
            {

                float FyRaw = -_lateralStiffness * vLat;
                

                float steeringResponse = Mathf.Clamp(_steerInput * 2f, -1f, 1f);
                FyRaw *= (1f + 0.3f * Mathf.Abs(steeringResponse));
                
                Fy += FyRaw;
            }

            // Ограничение силы трения
            float frictionLimit = _frictionCoefficient * normalForce;
            float forceLength = Mathf.Sqrt(Fx * Fx + Fy * Fy);
            if (forceLength > frictionLimit)
            {
                float scale = frictionLimit / forceLength;
                Fy *= scale;
                Fx *= scale;
            }

            Vector3 force = wheelForward * Fx + wheelRight * Fy;
            _rb.AddForceAtPosition(force, wheelPos, ForceMode.Force);

            
        }

        /*private void ApplyEngineForces()
        {
            Vector3 forward = transform.forward;
            float speedAlongForward = Vector3.Dot(_rb.linearVelocity, forward);
            
            if (_throttleInput > 0 && speedAlongForward > _maxSpeed) 
                return;
        }*/

        private void ApplyEngineForces()
        {
            Vector3 forward = transform.forward;
            float speedAlongForward = Vector3.Dot(_rb.linearVelocity, forward);
            if (_throttleInput > 0 && speedAlongForward > _maxSpeed) return;

            float driveTorque = _engineTorque * _throttleInput;

            float driveForcePerWheel = driveTorque / _wheelRadius / 2f;

            Vector3 forceRearLeft = forward * driveForcePerWheel;
            Vector3 forceRearRight = forward * driveForcePerWheel;

            _rb.AddForceAtPosition(forceRearLeft, _rearLeftWheel.position, ForceMode.Force);
            _rb.AddForceAtPosition(forceRearRight, _rearRightWheel.position, ForceMode.Force);
        }

        private void CheckGrounded()
        {
            Ray ray = new Ray(_groundCheckPoint.position, Vector3.down);
            _isGrounded = Physics.Raycast(ray, _groundCheckDistance, _groundLayerMask);
            
            Debug.DrawRay(ray.origin, ray.direction * _groundCheckDistance, _isGrounded ? Color.green : Color.red);

        }
    }
}