using KBCore.Refs;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMover : ValidatedMonoBehaviour
{
    [SerializeField] private float _movementSpeed = 5f;
    [SerializeField] private float _boostMultiplier = 1.5f;
    [SerializeField] private float _scrollSpeed = 0.2f;
    
    [SerializeField, Self] private Camera _camera;
    private InputAction _movementAction;
    private InputAction _boostAction;
    private InputAction _scrollAction;

    private Vector2 _movementValue;

    private void Start()
    {
        _movementAction = InputSystem.actions.FindAction("Movement", true);
        _boostAction = InputSystem.actions.FindAction("Speedboost", true);
        _scrollAction = InputSystem.actions.FindAction("Scroll", true);
    }

    private void Update()
    {
        float scrollValue = _scrollAction.ReadValue<float>();
        _camera.orthographicSize -= scrollValue * _scrollSpeed;
        
        this.HandleMovement();
    }

    private void HandleMovement()
    {
        _movementValue = _movementAction.ReadValue<Vector2>();
        bool isBoosted = _boostAction.IsPressed();
        
        Vector3 right = this.transform.right;
        Vector3 up = this.transform.up;

        Vector3 direction = (up * _movementValue.y) + (right * _movementValue.x);
        Vector3 movement = direction * (_movementSpeed * Time.deltaTime);
        movement *= isBoosted ? _boostMultiplier : 1f;
        
        this.transform.Translate(movement, Space.World);
    }
}
