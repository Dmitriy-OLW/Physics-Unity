using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class JetEngine : MonoBehaviour
{
    [SerializeField] private Transform _nozzle;

    [Header("Тяга")] 
    [SerializeField] private float _thrustDrySL = 79000f;
    [SerializeField] private float _thrustABSL = 129000f;
    [SerializeField] private float _throttleRate = 1.0f;
    [SerializeField] private float _throttleStep = 0.05f;

    [SerializeField] private InputMap _inputMap;
    private Rigidbody _rb;

    private float _throttle01;
    private bool _afterBurner;
    private float _speedMS;
    private float _lastAppliedThrust;

    private InputAction _throttlerUpHolder;
    private InputAction _throttlerDownHold;
    private InputAction _throttleStepUp;
    private InputAction _throttleStepDown;
    private InputAction _tooggleAB;

    private void OnEnable()
    {
        _throttlerUpHolder?.Enable();
        _throttlerDownHold?.Enable();
        _throttleStepUp?.Enable();
        _throttleStepDown?.Enable();
        _tooggleAB?.Enable();
    }

    private void OnDisable()
    {
        _throttlerUpHolder?.Disable();
        _throttlerDownHold?.Disable();
        _throttleStepUp?.Disable();
        _throttleStepDown?.Disable();
        _tooggleAB?.Disable();
    }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _inputMap = new InputMap();
        
        _throttle01 = 0.0f;
        _afterBurner = false;

        _throttlerUpHolder = _inputMap.JetEngine.throttlerUpHolder;
        _throttlerDownHold = _inputMap.JetEngine.throttlerDownHold;
        _throttleStepUp = _inputMap.JetEngine.throttleStepUp;
        _throttleStepDown = _inputMap.JetEngine.throttleStepDown;
        _tooggleAB = _inputMap.JetEngine.tooggleAB;

        _throttleStepUp.performed += _ => AdjustThrottle(_throttleStep);
        _throttleStepDown.performed += _ => AdjustThrottle(-_throttleStep);
        _tooggleAB.performed += _ =>
        {
            _afterBurner = !_afterBurner;
        };
    }
    private void AdjustThrottle(float delta)
    {
        _throttle01 = Mathf.Clamp01(_throttle01 + delta);
    }

    private void FixedUpdate()
    {
        _speedMS = _rb.linearVelocity.magnitude;
        

        float dt = Time.fixedDeltaTime;
        
        
        
        if (_throttlerUpHolder.IsPressed()) 
            _throttle01 = Mathf.Clamp01(_throttle01 + _throttleRate * dt);
        
        if (_throttlerDownHold.IsPressed()) 
            _throttle01 = Mathf.Clamp01(_throttle01 - _throttleRate * dt);


        float thrust = _throttle01 * (_afterBurner ? _thrustABSL : _thrustDrySL);
        _lastAppliedThrust = thrust;


        if (_nozzle != null && thrust > 0)
        {
            Vector3 force = _nozzle.forward * thrust;
            _rb.AddForceAtPosition(force, _nozzle.position, ForceMode.Impulse);
        }
    }
    
    void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 55;
        GUI.color = Color.black;
        GUILayout.Label($" ", style);
        GUILayout.Label($" ", style);

        GUILayout.Label($"AfterBurner: {_afterBurner}", style);
        GUILayout.Label($"Throttle: {_throttle01:0.0}", style);



    }

    
}