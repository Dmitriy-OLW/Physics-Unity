using UnityEngine;
using UnityEngine.InputSystem;

public class AircraftControl : MonoBehaviour
{
    [Header("Управление поворотами")]
    [SerializeField] private float _pitchTorque = 50000f; // Тангаж (W/S)
    [SerializeField] private float _rollTorque = 40000f;  // Рыскание (A/D)
    [SerializeField] private float _yawTorque = 30000f;   // Крен (Q/E)
    
    [Header("Сопротивление воздуха")]
    [SerializeField] private float _maxDrag = 10f; // Максимальное линейное сопротивление
    [SerializeField] private float _dragIncreaseSpeed = 2f; // Скорость увеличения сопротивления
    [SerializeField] private float _dragDecreaseSpeed = 1f; // Скорость уменьшения сопротивления
    
    private Rigidbody _rb;
    private InputMap _inputMap;
    
    private InputAction _wasdAction;
    private InputAction _qeAction;
    private InputAction _dragAction;

    private float _currentDrag = 0f;
    private bool _isDragActive = false;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _inputMap = new InputMap();
        
        _wasdAction = _inputMap.JetEngine.WASD;
        _qeAction = _inputMap.JetEngine.QE;
        _dragAction = _inputMap.JetEngine.throttleStepDown; // Используем клавишу Z

        // Настраиваем обработчики для кнопки Z
        _dragAction.started += _ => StartDrag();
        _dragAction.canceled += _ => StopDrag();
    }

    private void OnEnable()
    {
        _wasdAction?.Enable();
        _qeAction?.Enable();
        _dragAction?.Enable();
    }

    private void OnDisable()
    {
        _wasdAction?.Disable();
        _qeAction?.Disable();
        _dragAction?.Disable();
    }

    private void StartDrag()
    {
        _isDragActive = true;
    }

    private void StopDrag()
    {
        _isDragActive = false;
    }

    private void FixedUpdate()
    {
        ApplyRotationForces();
        UpdateAirDrag();
    }

    private void ApplyRotationForces()
    {
        Vector2 wasdInput = _wasdAction.ReadValue<Vector2>();
        float qeInput = _qeAction.ReadValue<float>();
        
        // W/S - тангаж (вокруг оси X) - кивание носом вверх/вниз
        if (wasdInput.y != 0)
        {
            Vector3 pitchTorque = transform.forward * (-wasdInput.y * _pitchTorque * Time.fixedDeltaTime);
            _rb.AddTorque(pitchTorque, ForceMode.Impulse);
        }

        // A/D - рыскание (вокруг оси Y) - поворот носом влево/вправо
        if (wasdInput.x != 0)
        {
            Vector3 rollTorque = transform.right * (-wasdInput.x * _rollTorque * Time.fixedDeltaTime);
            _rb.AddTorque(rollTorque, ForceMode.Impulse);
        }

        // Q/E - крен (вокруг оси Z) - наклон крыльев влево/вправо
        if (qeInput != 0)
        {
            Vector3 yawTorque = transform.up * (qeInput * _yawTorque * Time.fixedDeltaTime);
            _rb.AddTorque(yawTorque, ForceMode.Impulse);
        }
    }

    private void UpdateAirDrag()
    {
        if (_isDragActive)
        {
            _currentDrag = Mathf.MoveTowards(_currentDrag, _maxDrag, _dragIncreaseSpeed * Time.fixedDeltaTime);
        }
        else
        {
            _currentDrag = Mathf.MoveTowards(_currentDrag, 0f, _dragDecreaseSpeed * Time.fixedDeltaTime);
        }
        
        _rb.linearDamping = _currentDrag;
        

    }

    private void OnGUI()
    {
        
        GUIStyle style = new GUIStyle();
        style.fontSize = 55;
        GUI.color = Color.black;
        
        GUILayout.Label($" ", style);
        GUILayout.Label($" ", style);
        
        GUILayout.Label($" ", style);
        GUILayout.Label($" ", style);
        GUILayout.Label($"Air Drag: {_currentDrag:0.00}", style);
        GUILayout.Label($"brake Activated: {_isDragActive}", style);

    }
}