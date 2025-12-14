using UnityEngine;

namespace Kart
{

    [CreateAssetMenu(fileName = "KartConfig", menuName = "Kart/Kart Configuration")]
    public class KartConfig : ScriptableObject
    {
        [Header("Mass & Physics")]
        public float mass = 800f;
        public float frictionCoefficient = 1.0f;
    
        [Header("Suspension & Weight")]
        public float frontAxleShare = 0.5f;
    
        [Header("Steering")]
        public float maxSteerAngle = 30f;
    
        [Header("Tyre Properties")]
        public float frontLateralStiffness = 80f;
        public float rearLateralStiffness = 80f;
        public float rollingResistance = 0.5f;
    
        [Header("Engine")]
        public AnimationCurve engineTorqueCurve;
        public float engineInertia = 0.2f;
        public float maxRpm = 8000f;
        public float idleRpm = 1000f;
        public float revLimiterRpm = 7500f;
        public float throttleResponse = 5f;
        public float engineFrictionCoeff = 0.02f;
        public float loadTorqueCoeff = 5f;
        public float baseTorque = 400f;
    
        [Header("Drivetrain")]
        public float gearRatio = 8f;
        public float drivetrainEfficiency = 0.9f;
        public float wheelRadius = 0.3f;
        public float maxSpeed = 20f;
        
        [Header("Physics Settings")] 
        public float worldGravity = 9.81f;
        [Header("HandBrake")] 
        public float handbrakeRearStiffness = 0f;
        public float handbrakeResistanceMultiplier = 100f;
        public float handbrakeLateralMultiplier = 5f;
    
        private void OnValidate()
        {
            if (engineTorqueCurve == null || engineTorqueCurve.keys.Length == 0)
            {
                engineTorqueCurve = AnimationCurve.EaseInOut(0, 1000, 2, 8000);
            }
        }
    }
}