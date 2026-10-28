using UnityEngine;

namespace Sorchemist.UI
{
    // Parmakla sürüklenen alan (karıştırma)
    public class DragArea : MonoBehaviour, UnityEngine.EventSystems.IDragHandler,
        UnityEngine.EventSystems.IPointerDownHandler, UnityEngine.EventSystems.IPointerUpHandler
    {
        public System.Action<Vector2> OnDragLocal;   // RectTransform içi yerel konum
        public System.Action OnRelease;

        void Report(UnityEngine.EventSystems.PointerEventData e)
        {
            Vector2 local;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, e.position,
                    e.pressEventCamera, out local) && OnDragLocal != null)
                OnDragLocal(local);
        }

        public void OnPointerDown(UnityEngine.EventSystems.PointerEventData e) { Report(e); }
        public void OnDrag(UnityEngine.EventSystems.PointerEventData e) { Report(e); }
        public void OnPointerUp(UnityEngine.EventSystems.PointerEventData e) { if (OnRelease != null) OnRelease(); }
    }
}
