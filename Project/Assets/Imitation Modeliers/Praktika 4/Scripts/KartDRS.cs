using UnityEngine;
using UnityEngine.InputSystem;

namespace Bolide
{
    public class KartDRS : MonoBehaviour
    {
        [Header("DRS Configuration")]
        [SerializeField] private Transform rearWing; // антикрыло
        [SerializeField] private float normalAngle = 15f; // Нормальный угол атаки
        [SerializeField] private float drsAngle = -3f; // Угол при включенном DRS
        [SerializeField] private float transitionSpeed = 5f; // Скорость изменения угла
        
        [Header("Input")]
        [SerializeField] private InputActionReference drsActionRef; // Кнопка DRS

        private KartAero _kartAero; 
        private float _targetAngle;
        private float _currentAngle;
        private bool _drsActive;
        
        public bool IsDrsActive => _drsActive;
        public float CurrentAngle => _currentAngle;
        
        private void Awake()
        {
            _kartAero = GetComponent<KartAero>();
            
            if (rearWing == null)
            {
                enabled = false;
                return;
            }
            
            _targetAngle = normalAngle;
            _currentAngle = normalAngle;
            _drsActive = false;
        }
        
        private void OnEnable()
        {
            if (drsActionRef != null)
                drsActionRef.action.Enable();
        }
        
        private void OnDisable()
        {
            if (drsActionRef != null)
                drsActionRef.action.Disable();
        }
        
        private void Update()
        {
            ReadDRSInput();
            UpdateWingAngle();
        }
        
        private void ReadDRSInput()
        {
            if (drsActionRef != null && drsActionRef.action != null)
            {
                bool pressed = drsActionRef.action.WasPressedThisFrame();
                if (pressed)
                {
                    ToggleDRS();
                }
            }
        }
        
        private void ToggleDRS()
        {
            _drsActive = !_drsActive;
            _targetAngle = _drsActive ? drsAngle : normalAngle;
            
            Debug.Log($"DRS: {(_drsActive ? "ON" : "OFF")}");
        }
        
        private void UpdateWingAngle()
        {
            _currentAngle = Mathf.Lerp(_currentAngle, _targetAngle, 
                transitionSpeed * Time.deltaTime);
            
            if (rearWing != null)
            {
                Vector3 localRot = rearWing.localEulerAngles;
                localRot.x = _currentAngle;
                rearWing.localEulerAngles = localRot;
            }
            
            if (_kartAero != null)
                _kartAero.SetWingAngle(_currentAngle);
        }
        
        private void OnGUI()
        {
            GUI.color = Color.magenta;
            GUILayout.BeginArea(new Rect(320, 80, 300, 90), GUI.skin.box);
            GUI.color = Color.white;
            GUILayout.BeginVertical("Box");
            
            GUILayout.Label($"DRS: {(_drsActive ? "ON" : "OFF")}");
            GUILayout.Label($"Wing Angle: {_currentAngle:F1}°");

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}