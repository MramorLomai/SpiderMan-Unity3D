using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JoystickReader : MonoBehaviour
{
    public Vector2 touchDirection = Vector2.zero;
    public Joystick joystick;

    private void Start()
    {
        // Подписываемся на статическое событие в JoyStick.cs
        Joystick.onJoyStickMoved += GetJoyStickDirection;
    }

    private void OnDestroy()
    {
        // Отписываемся от события при уничтожении объекта
        Joystick.onJoyStickMoved -= GetJoyStickDirection;
    }

    void GetJoyStickDirection(Vector2 touchPosition)
    {
        touchDirection = touchPosition;
        //inputHandler.OnJoystickMoved(touchDirection);
    }
}