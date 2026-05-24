using UnityEngine;
using UnityEngine.InputSystem;

namespace GameFramework
{
    public class InputService : IInputService, System.IDisposable
    {
        InputAction _moveAction;
        InputAction _jumpAction;
        InputAction _attackAction;
        InputAction _interactAction;

        public Vector2 MoveDirection => _moveAction?.ReadValue<Vector2>() ?? Vector2.zero;

        public InputService()
        {
            _moveAction = new InputAction("Move", InputActionType.Value, "<Gamepad>/leftStick");
            _moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            _jumpAction = new InputAction("Jump", InputActionType.Button, "<Keyboard>/space");
            _attackAction = new InputAction("Attack", InputActionType.Button, "<Mouse>/leftButton");
            _interactAction = new InputAction("Interact", InputActionType.Button, "<Keyboard>/e");

            _moveAction.Enable();
            _jumpAction.Enable();
            _attackAction.Enable();
            _interactAction.Enable();
        }

        public bool IsActionPressed(string actionName)
        {
            return actionName switch
            {
                "Jump" => _jumpAction.IsPressed(),
                "Attack" => _attackAction.IsPressed(),
                "Interact" => _interactAction.IsPressed(),
                _ => false
            };
        }

        public void Dispose()
        {
            _moveAction?.Disable();
            _jumpAction?.Disable();
            _attackAction?.Disable();
            _interactAction?.Disable();
        }
    }
}
