using UnityEngine;

namespace GameFramework
{
    public interface IInputService : IService
    {
        Vector2 MoveDirection { get; }
        bool IsActionPressed(string actionName);
    }
}
