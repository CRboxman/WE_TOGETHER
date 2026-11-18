using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputSystemTest : MonoBehaviour
{
    public InputAction Fire;
    public InputAction Move;
    // Start is called before the first frame update
    void Start()
    {
        Move.Enable();
        Move.started += ctx => Debug.Log("Move started");
        Move.performed += ctx => Debug.Log("Move performed");
        Move.canceled += ctx => Debug.Log("Move canceled");
    }

    // Update is called once per frame
    void Update()
    {
        // if(Keyboard.current.spaceKey.wasPressedThisFrame)
        // {
        //     Debug.Log("Space Key Pressed");
        // }
        //
        // if (Mouse.current.leftButton.wasPressedThisFrame)
        // {
        //     Debug.Log("Mouse Left Button Pressed");
        // }
        //
        // if (Keyboard.current.wKey.isPressed)
        // {
        //     Debug.Log("W Key is being held down");
        // }
        print(Move.ReadValue<float>());
    } 
}
