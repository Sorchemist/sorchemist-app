using UnityEngine;

namespace Sorchemist.UI
{
    // Yavaşça dönen ve nefes alan görsel (sonuç penceresindeki efekt)
    public class Spinner : MonoBehaviour
    {
        public float Speed = 25f;
        void Update()
        {
            transform.Rotate(0, 0, -Speed * Time.deltaTime);
            transform.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * 2.5f) * 0.05f);
        }
    }
}
