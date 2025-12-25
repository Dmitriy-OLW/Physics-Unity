using UnityEngine;

namespace Bolide
{
    public class KartAero : MonoBehaviour
    {
        [Header("Aero Drag")]
        [SerializeField] private float airDensity = 1.225f;
        [SerializeField] private float dragCoefficient = 0.9f; // Cx
        [SerializeField] private float frontalArea = 0.6f;     // A (м²)

        [Header("Rear Wing")]
        [SerializeField] private Transform rearWing;
        [SerializeField] private float wingArea = 0.4f; // м²
        [SerializeField] private float liftCoefficientSlope = 0.05f; // k
        [SerializeField] private float wingAngleDeg = 10f; // угол атаки
        
        [Header("Ground Effect")]
        [SerializeField] private float groundEffectStrength = 3000f;
        [SerializeField] private float groundRayLength = 1.0f;
        
        private Rigidbody rb;

        private float _сurrentDrag;
        private float _currentDownforce;

        private void Awake() => rb = GetComponent<Rigidbody>();

        private void FixedUpdate()
        {
            ApplyDrag();
            ApplyWingDownforce();
            ApplyGroundEffect();
        }
        
        private void ApplyDrag()
        {
            Vector3 v = rb.linearVelocity;
            float speed = v.magnitude;

            if (speed < 0.01f)
                return;

            float dragForce = 0.5f * airDensity * dragCoefficient * frontalArea * speed * speed;

            Vector3 drag = -v.normalized * dragForce;

            rb.AddForce(drag, ForceMode.Force);
            
            _сurrentDrag = dragForce;
        }
        
        private void ApplyWingDownforce()
        {
            if (rearWing == null) return;

            float speed = rb.linearVelocity.magnitude;
            if (speed < 0.01f) return;

            float alphaRad = wingAngleDeg * Mathf.Deg2Rad;
            float Cl = liftCoefficientSlope * alphaRad;

            float downforce = 0.5f * airDensity * Cl * wingArea * speed * speed;

            Vector3 force = -transform.up * downforce;

            rb.AddForceAtPosition(force, rearWing.position, ForceMode.Force);
            
            _currentDownforce = downforce;
        }
        
        private void ApplyGroundEffect()
        {
            RaycastHit hit;

            if (Physics.Raycast(transform.position, -transform.up, out hit, groundRayLength))
            {
                float h = hit.distance; // высота над землёй
                if (h < 0.01f) h = 0.01f;

                float geForce = groundEffectStrength / h;

                Vector3 force = -transform.up * geForce;

                rb.AddForce(force, ForceMode.Force);
            }
        }
        
        public void SetWingAngle(float newAngle)
        {
            wingAngleDeg = newAngle;
        }
        
        private void OnGUI()
        {
            GUI.color = Color.cyan;
            GUILayout.BeginArea(new Rect(320, 10, 300, 60), GUI.skin.box);
            GUI.color = Color.white;
            GUILayout.BeginVertical("Box");
            GUILayout.Label($"Drag: {_сurrentDrag:F0} N");
            GUILayout.Label($"Downforce: {_currentDownforce:F0} N");

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}