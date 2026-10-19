using UnityEngine;
using UnityEngine.InputSystem;

namespace UltimaJugada
{
    public static class GameInput
    {
        public static Vector2 Move
        {
            get
            {
                var k = Keyboard.current;
                if (k == null) return Vector2.zero;
                float x = (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1f : 0f) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1f : 0f);
                float y = (k.wKey.isPressed || k.upArrowKey.isPressed ? 1f : 0f) - (k.sKey.isPressed || k.downArrowKey.isPressed ? 1f : 0f);
                return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
            }
        }
        public static Vector2 MousePosition => Mouse.current?.position.ReadValue() ?? Vector2.zero;
        public static bool Clicked => Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        public static bool Released => Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
    }
}
