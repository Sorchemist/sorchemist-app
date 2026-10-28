using UnityEngine;

namespace Sorchemist.Game
{
    // Çentikli (notch) telefonlarda arayüzü güvenli alana sığdırır
    public class SafeArea : MonoBehaviour
    {
        Rect _last;
        void Update()
        {
            var sa = Screen.safeArea;
            if (sa == _last || Screen.width == 0) return;
            _last = sa;
            var rt = (RectTransform)transform;
            rt.anchorMin = new Vector2(sa.xMin / Screen.width, sa.yMin / Screen.height);
            rt.anchorMax = new Vector2(sa.xMax / Screen.width, sa.yMax / Screen.height);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
