using UnityEngine;
using UnityEngine.InputSystem;

namespace Kart
{
    [RequireComponent(typeof(Rigidbody))]
    public class KartController : MonoBehaviour
    {
        [Header("Configuration")] [SerializeField]
        private KartConfig _config;

        [Header("Wheel attachment points")] [SerializeField]
        private Transform _frontLeftWheel;

        [SerializeField] private Transform _frontRightWheel;
        [SerializeField] private Transform _rearLeftWheel;
        [SerializeField] private Transform _rearRightWheel;

        [Header("Engine & drivetrain")] [SerializeField]
        private KartEngine _engine;

        [Header("Input")] [SerializeField]
        private InputActionReference _moveActionRef;

        [SerializeField] private InputActionReference _handbrakeActionRef;
        [SerializeField] private InputActionReference _respawnActionRef;

        [Header("Telemetry")] [SerializeField] 
        private bool _showTelemetry = true;
        
        [Header("Ground Detection")]
        [SerializeField] private LayerMask _groundLayerMask = 1;
        [SerializeField] private float _groundCheckDistance = 0.5f;
        
        private Rigidbody _rb;
        private float _frontLeftNormalForce;
        private float _frontRightNormalForce;
        private float _rearLeftNormalForce;
        private float _rearRightNormalForce;

        private Quaternion _frontLeftInitialLocalRot;
        private Quaternion _frontRightInitialLocalRot;

        private float _throttleInput;
        private float _steerInput;
        private bool _handbrakeActive;

        private float _frictionCoefficient = 1.0f;
        private float _frontLateralStiffness = 80f;
        private float _rearLateralStiffness = 80f;
        private float _rollingResistance = 0.5f;
        private float _maxSteerAngle = 30f;
        private float _gearRatio = 8f;
        private float _drivetrainEfficiency = 0.9f;
        private float _wheelRadius = 0.3f;
        private float _maxSpeed = 20f;
        private float _maxReverseSpeed = 10f;
        private float _frontAxleShare = 0.5f;
        
        private float _worldGravity = 9.81f;
     
        private float _handbrakeRearStiffness = 0f;
        private float _handbrakeResistanceMultiplier = 100f;
        private float _handbrakeLateralMultiplier = 5f;

        public float Speed { get; private set; }
        public float SpeedKPH { get; private set; }
        public float RearAxleForceX { get; private set; }
        public float FrontAxleForceY { get; private set; }
        public float[] WheelVLat { get; private set; } = new float[4];

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            
            if (_config != null)
            {
                ApplyConfig(_config);
            }

            InitializeWheelData();
        }

        private void Start()
        {
            ComputeStaticWheelLoads();
        }

        public void ApplyConfig(KartConfig config)
        {
            _config = config;
            
            _rb.mass = config.mass;

            _frictionCoefficient = config.frictionCoefficient;
            _frontLateralStiffness = config.frontLateralStiffness;
            _rearLateralStiffness = config.rearLateralStiffness;
            _rollingResistance = config.rollingResistance;
            _maxSteerAngle = config.maxSteerAngle;
            _gearRatio = config.gearRatio;
            _drivetrainEfficiency = config.drivetrainEfficiency;
            _wheelRadius = config.wheelRadius;
            _maxSpeed = config.maxSpeed;
            _maxReverseSpeed = config.maxSpeed * 0.5f;
            _frontAxleShare = config.frontAxleShare;
            _worldGravity = config.worldGravity;
            _handbrakeRearStiffness = config.handbrakeRearStiffness;
            _handbrakeResistanceMultiplier = config.handbrakeResistanceMultiplier;
            _handbrakeLateralMultiplier = config.handbrakeLateralMultiplier;
            
            if (_engine != null)
            {
                _engine.ApplyConfig(config);
            }
            
            ComputeStaticWheelLoads();
        }

        private void InitializeWheelData()
        {
            if (_frontLeftWheel != null)
                _frontLeftInitialLocalRot = _frontLeftWheel.localRotation;

            if (_frontRightWheel != null)
                _frontRightInitialLocalRot = _frontRightWheel.localRotation;
        }

        private void OnEnable()
        {
            if (_moveActionRef != null)
                _moveActionRef.action.Enable();
            if (_handbrakeActionRef != null)
                _handbrakeActionRef.action.Enable();
            if(_respawnActionRef != null)
                _respawnActionRef.action.Enable();
        }

        private void OnDisable()
        {
            if (_moveActionRef != null)
                _moveActionRef.action.Disable();
            if (_handbrakeActionRef != null)
                _handbrakeActionRef.action.Disable();
            if(_respawnActionRef != null)
                _respawnActionRef.action.Disable();
        }

        private void Update()
        {
            ReadInput();
            RotateFrontWheels();
            UpdateTelemetry();
            if (_respawnActionRef != null && _respawnActionRef.action.WasPressedThisFrame())
            {
                transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
                transform.position += Vector3.up * 1f;
            }
        }

        private void FixedUpdate()
        {
            ApplyWheelForces();
        }

        private void ComputeStaticWheelLoads()
        {
            float mass = _rb.mass;
            float totalWeight = mass * _worldGravity;

            float frontWeight = totalWeight * _frontAxleShare;
            float rearWeight = totalWeight * (1f - _frontAxleShare);

            _frontLeftNormalForce = frontWeight * 0.5f;
            _frontRightNormalForce = frontWeight * 0.5f;

            _rearLeftNormalForce = rearWeight * 0.5f;
            _rearRightNormalForce = rearWeight * 0.5f;
        }

        private void ReadInput()
        {
            if (_moveActionRef != null && _moveActionRef.action != null)
            {
                Vector2 move = _moveActionRef.action.ReadValue<Vector2>();
                _steerInput = Mathf.Clamp(move.x, -1f, 1f);
                _throttleInput = Mathf.Clamp(move.y, -1f, 1f);
            }
            else
            {
                _steerInput = 0f;
                _throttleInput = 0f;
            }

            if (_handbrakeActionRef != null && _handbrakeActionRef.action != null)
            {
                _handbrakeActive = _handbrakeActionRef.action.ReadValue<float>() > 0.5f;
            }
            else
            {
                _handbrakeActive = false;
            }
        }

        private void RotateFrontWheels()
        {
            float steerAngle = _maxSteerAngle * _steerInput;
            Quaternion steerRotation = Quaternion.Euler(0f, steerAngle, 0f);

            if (_frontLeftWheel != null)
                _frontLeftWheel.localRotation = _frontLeftInitialLocalRot * steerRotation;

            if (_frontRightWheel != null)
                _frontRightWheel.localRotation = _frontRightInitialLocalRot * steerRotation;
        }

        private void ApplyWheelForces()
        {
            RearAxleForceX = 0f;
            FrontAxleForceY = 0f;

            ApplyWheelForce(_frontLeftWheel, _frontLeftNormalForce, isFront: true, isLeft: true, wheelIndex: 0);
            ApplyWheelForce(_frontRightWheel, _frontRightNormalForce, isFront: true, isLeft: false, wheelIndex: 1);
            ApplyWheelForce(_rearLeftWheel, _rearLeftNormalForce, isFront: false, isLeft: true, wheelIndex: 2);
            ApplyWheelForce(_rearRightWheel, _rearRightNormalForce, isFront: false, isLeft: false, wheelIndex: 3);
        }

        private void ApplyWheelForce(Transform wheel, float normalForce, bool isFront, bool isLeft, int wheelIndex)
        {
            if (wheel == null || _rb == null) return;
            
            Ray ray = new Ray(wheel.position, Vector3.down);
            bool isGrounded = Physics.Raycast(ray, _groundCheckDistance, _groundLayerMask);
        
            Debug.DrawRay(ray.origin, ray.direction * _groundCheckDistance, isGrounded ? Color.green : Color.red);

            if (!isGrounded) return;

            Vector3 wheelPos = wheel.position;
            Vector3 wheelForward = wheel.forward;
            Vector3 wheelRight = wheel.right;

            Vector3 v = _rb.GetPointVelocity(wheelPos);
            float vLong = Vector3.Dot(v, wheelForward);
            float vLat = Vector3.Dot(v, wheelRight);

            WheelVLat[wheelIndex] = vLat;

            float Fx = 0f;
            float Fy = 0f;
            
            if (!isFront && _engine != null)
            {
                float speedAlongForward = Vector3.Dot(_rb.linearVelocity, transform.forward);
                float driveDirection = Mathf.Sign(_throttleInput);
                
                bool canAccelerateForward = _throttleInput > 0f && speedAlongForward < _maxSpeed;
                bool canAccelerateBackward = _throttleInput < 0f && speedAlongForward > -_maxReverseSpeed;
                
                if (canAccelerateForward || canAccelerateBackward)
                {
                    float engineTorque = _engine.Simulate(Mathf.Abs(_throttleInput), speedAlongForward, Time.fixedDeltaTime);
                    float totalWheelTorque = engineTorque * _gearRatio * _drivetrainEfficiency;
                    float wheelTorque = totalWheelTorque * 0.5f;
                    Fx += driveDirection * wheelTorque / _wheelRadius;
                }
            }
            
            Fx += -_rollingResistance * vLong;
            
            float lateralStiffness = isFront ? _frontLateralStiffness : _rearLateralStiffness;

            if (_handbrakeActive && !isFront)
            {
                lateralStiffness = _handbrakeRearStiffness;
                Fx += -_rollingResistance * _handbrakeResistanceMultiplier * vLong;
                Fy += -_frontLateralStiffness * _handbrakeLateralMultiplier * vLat;
            }
            else
            {
                Fy += -lateralStiffness * vLat;
            }
            
            float frictionLimit = _frictionCoefficient * normalForce;
            float forceLength = Mathf.Sqrt(Fx * Fx + Fy * Fy);

            if (forceLength > frictionLimit && forceLength > 1e-6f)
            {
                float scale = frictionLimit / forceLength;
                Fx *= scale;
                Fy *= scale;
            }
            
            if (isFront)
            {
                FrontAxleForceY += Mathf.Abs(Fy);
            }
            
            if (!isFront)
            {
                RearAxleForceX += Fx;
            }
            
            Vector3 force = wheelForward * Fx + wheelRight * Fy;
            _rb.AddForceAtPosition(force, wheelPos, ForceMode.Force);
        }

        private void UpdateTelemetry()
        {
            Speed = _rb.linearVelocity.magnitude;
            SpeedKPH = Speed * 3.6f;
        }

        private void OnGUI()
        {
            if (!_showTelemetry) return;

            GUI.color = Color.black;
            GUILayout.BeginArea(new Rect(12, 10, 300, 400), GUI.skin.box);
            GUI.color = Color.white;
            GUILayout.BeginVertical("Box");

            GUILayout.Label($"Speed: {Speed:F1} m/s ({SpeedKPH:F1} km/h)");
            GUILayout.Label($"RPM: {(_engine != null ? _engine.CurrentRpm.ToString("F0") : "N/A")}");
            GUILayout.Label($"Torque: {(_engine != null ? _engine.CurrentTorque.ToString("F0") : "N/A")} N·m");
            GUILayout.Label($"Rear Axle Fx: {RearAxleForceX:F0} N");
            GUILayout.Label($"Front Axle Fy: {FrontAxleForceY:F0} N");
            GUILayout.Label($"Throttle: {_throttleInput:F2}");
            GUILayout.Label($"Steer: {_steerInput:F2}");
            GUILayout.Label($"Handbrake: {_handbrakeActive}");

            GUILayout.Label("Wheel vLat:");
            GUILayout.Label($"  FL: {WheelVLat[0]:F2}");
            GUILayout.Label($"  FR: {WheelVLat[1]:F2}");
            GUILayout.Label($"  RL: {WheelVLat[2]:F2}");
            GUILayout.Label($"  RR: {WheelVLat[3]:F2}");
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}