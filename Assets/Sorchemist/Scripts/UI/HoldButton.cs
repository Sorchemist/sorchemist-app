using UnityEngine;

namespace Sorchemist.UI
{
    // Basılı tutulabilen buton (körük, ısıtıcı...)
    public class HoldButton : MonoBehaviour, UnityEngine.EventSystems.IPointerDownHandler,
        UnityEngine.EventSystems.IPointerUpHandler, UnityEngine.EventSystems.IPointerExitHandler
    {
        public bool Held;
        public void OnPointerDown(UnityEngine.EventSystems.PointerEventData e) { Held = true; }
        public void OnPointerUp(UnityEngine.EventSystems.PointerEventData e) { Held = false; }
        public void OnPointerExit(UnityEngine.EventSystems.PointerEventData e) { Held = false; }
    }
}
