using UnityEngine;
#if ENABLE_INPUT_SYSTEM && OVILLO_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DesenredaElOvillo.Juego
{
    /// <summary>
    /// Única puerta de entrada del ratón y el teclado. Funciona con el Input System nuevo (si el paquete está
    /// instalado y activo) o con el Input Manager clásico. Para añadir mando más adelante, solo se toca este archivo.
    /// </summary>
    public static class EntradaJugador
    {
        public static Vector2 PosicionRaton
        {
            get
            {
#if ENABLE_INPUT_SYSTEM && OVILLO_INPUT_SYSTEM
                return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
                return Input.mousePosition;
#endif
            }
        }

        public static bool IzquierdoPulsado
        {
            get
            {
#if ENABLE_INPUT_SYSTEM && OVILLO_INPUT_SYSTEM
                return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
                return Input.GetMouseButtonDown(0);
#endif
            }
        }

        public static bool IzquierdoMantenido
        {
            get
            {
#if ENABLE_INPUT_SYSTEM && OVILLO_INPUT_SYSTEM
                return Mouse.current != null && Mouse.current.leftButton.isPressed;
#else
                return Input.GetMouseButton(0);
#endif
            }
        }

        public static bool IzquierdoSoltado
        {
            get
            {
#if ENABLE_INPUT_SYSTEM && OVILLO_INPUT_SYSTEM
                return Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
#else
                return Input.GetMouseButtonUp(0);
#endif
            }
        }

        public static bool DerechoPulsado
        {
            get
            {
#if ENABLE_INPUT_SYSTEM && OVILLO_INPUT_SYSTEM
                return Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
#else
                return Input.GetMouseButtonDown(1);
#endif
            }
        }

        public static bool Espacio
        {
            get
            {
#if ENABLE_INPUT_SYSTEM && OVILLO_INPUT_SYSTEM
                return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#else
                return Input.GetKeyDown(KeyCode.Space);
#endif
            }
        }

        public static bool Escape
        {
            get
            {
#if ENABLE_INPUT_SYSTEM && OVILLO_INPUT_SYSTEM
                return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
                return Input.GetKeyDown(KeyCode.Escape);
#endif
            }
        }

        /// <summary>Retroceso: deshace el último tramo de lana.</summary>
        public static bool Deshacer
        {
            get
            {
#if ENABLE_INPUT_SYSTEM && OVILLO_INPUT_SYSTEM
                return Keyboard.current != null && (Keyboard.current.backspaceKey.wasPressedThisFrame ||
                                                    (Keyboard.current.ctrlKey.isPressed && Keyboard.current.zKey.wasPressedThisFrame));
#else
                return Input.GetKeyDown(KeyCode.Backspace) ||
                       ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.Z));
#endif
            }
        }

        /// <summary>Teclas 1-5: elegir herramienta del costurero.</summary>
        public static int NumeroPulsado
        {
            get
            {
#if ENABLE_INPUT_SYSTEM && OVILLO_INPUT_SYSTEM
                if (Keyboard.current == null) return -1;
                if (Keyboard.current.digit1Key.wasPressedThisFrame) return 1;
                if (Keyboard.current.digit2Key.wasPressedThisFrame) return 2;
                if (Keyboard.current.digit3Key.wasPressedThisFrame) return 3;
                if (Keyboard.current.digit4Key.wasPressedThisFrame) return 4;
                if (Keyboard.current.digit5Key.wasPressedThisFrame) return 5;
                return -1;
#else
                for (int i = 1; i <= 5; i++)
                    if (Input.GetKeyDown(KeyCode.Alpha0 + i)) return i;
                return -1;
#endif
            }
        }
    }
}
