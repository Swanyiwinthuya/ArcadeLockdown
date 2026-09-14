using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

namespace ArcadeLockdown
{
    // Supports both a new URP template and an existing legacy-input project.
    public static class ArcadeInput
    {
        public static bool GetKey(KeyCode code)
        {
#if ENABLE_INPUT_SYSTEM
            return KeyControlFor(code)?.isPressed ?? false;
#else
            return UnityEngine.Input.GetKey(code);
#endif
        }
        public static bool GetKeyDown(KeyCode code)
        {
#if ENABLE_INPUT_SYSTEM
            return KeyControlFor(code)?.wasPressedThisFrame ?? false;
#else
            return UnityEngine.Input.GetKeyDown(code);
#endif
        }
        public static float GetAxisRaw(string axis) => GetAxis(axis);
        public static float GetAxis(string axis)
        {
#if ENABLE_INPUT_SYSTEM
            if(Mouse.current==null)return 0;
            if(axis=="Mouse X")return Mouse.current.delta.ReadValue().x*.075f;
            if(axis=="Mouse Y")return Mouse.current.delta.ReadValue().y*.075f;
            if(axis=="Mouse ScrollWheel")return Mouse.current.scroll.ReadValue().y/1200f;
            return 0;
#else
            return UnityEngine.Input.GetAxis(axis);
#endif
        }
#if ENABLE_INPUT_SYSTEM
        private static KeyControl KeyControlFor(KeyCode code)
        {
            if(Keyboard.current==null)return null;
            string name=code.ToString();
            if(name.StartsWith("Alpha"))name="Digit"+name.Substring(5);
            else if(name.StartsWith("Keypad"))name="Numpad"+name.Substring(6);
            if(code==KeyCode.Return)name="Enter";
            return Enum.TryParse(name,out Key key)?Keyboard.current[key]:null;
        }
#endif
    }
}
