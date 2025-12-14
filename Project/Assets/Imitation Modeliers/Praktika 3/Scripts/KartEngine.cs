using UnityEngine;

namespace Kart
{
    public class KartEngine : MonoBehaviour
    {
        [Header("Configuration")] [SerializeField]
        private KartConfig _config;
        
        private float _idleRpm = 1000f;
        private float _maxRpm = 8000f;
        private float _revLimiterRpm = 7500f;
        private AnimationCurve _torqueCurve;
        private float _flywheelInertia = 0.2f;
        private float _throttleResponse = 5f;
        private float _engineFrictionCoeff = 0.02f;
        private float _loadTorqueCoeff = 5f;
        private float _baseTorque = 500f;

        public float CurrentRpm { get; private set; }
        public float CurrentTorque { get; private set; }
        public float SmoothedThrottle { get; private set; }
        public float RevLimiterFactor { get; private set; } = 1f;

        private float _invInertiaFactor;

        private void Awake()
        {
            if (_config != null)
            {
                ApplyConfig(_config);
            }

            CurrentRpm = _idleRpm;
            UpdateInertiaFactor();
        }
        
        public void ApplyConfig(KartConfig config)
        {
            _config = config;

            _idleRpm = config.idleRpm;
            _maxRpm = config.maxRpm;
            _revLimiterRpm = config.revLimiterRpm;
            _torqueCurve = config.engineTorqueCurve;
            _flywheelInertia = config.engineInertia;
            _throttleResponse = config.throttleResponse;
            _engineFrictionCoeff = config.engineFrictionCoeff;
            _loadTorqueCoeff = config.loadTorqueCoeff;
            _baseTorque = config.baseTorque;

            UpdateInertiaFactor();
        }

        private void UpdateInertiaFactor()
        {
            _invInertiaFactor = 60f / (2f * Mathf.PI * Mathf.Max(_flywheelInertia, 0.0001f));
        }

        public float Simulate(float throttleInput, float forwardSpeed, float deltaTime)
        {
            float targetThrottle = Mathf.Clamp01(throttleInput);
            SmoothedThrottle = Mathf.MoveTowards(SmoothedThrottle, targetThrottle, _throttleResponse * deltaTime);

            UpdateRevLimiterFactor();

            float maxTorqueAtRpm = GetTorqueFromCurve(CurrentRpm);
            float effectiveThrottle = SmoothedThrottle * RevLimiterFactor;
            float driveTorque = maxTorqueAtRpm * effectiveThrottle;

            float frictionTorque = _engineFrictionCoeff * CurrentRpm;
            float loadTorque = _loadTorqueCoeff * Mathf.Abs(forwardSpeed);

            float netTorque = driveTorque - frictionTorque - loadTorque;

            float rpmDot = netTorque * _invInertiaFactor;
            CurrentRpm += rpmDot * deltaTime;

            if (CurrentRpm < _idleRpm) CurrentRpm = _idleRpm;
            if (CurrentRpm > _maxRpm) CurrentRpm = _maxRpm;

            CurrentTorque = driveTorque;
            return CurrentTorque;
        }
        
        private float GetTorqueFromCurve(float rpm)
        {
            if (_torqueCurve != null && _torqueCurve.length > 0)
            {
                float torqueValue = _torqueCurve.Evaluate(rpm);
                if (torqueValue > 0.01f)
                    return torqueValue;
            }
            
            return _baseTorque;
        }


        private void UpdateRevLimiterFactor()
        {
            if (CurrentRpm <= _revLimiterRpm)
            {
                RevLimiterFactor = 1f;
                return;
            }

            if (CurrentRpm >= _maxRpm)
            {
                RevLimiterFactor = 0f;
                return;
            }

            float t = (CurrentRpm - _revLimiterRpm) / (_maxRpm - _revLimiterRpm);
            RevLimiterFactor = 1f - t;
        }
    }
}