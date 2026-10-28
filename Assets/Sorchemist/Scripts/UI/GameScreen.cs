using Sorchemist.Core;
using Sorchemist.Game;
using UnityEngine;

namespace Sorchemist.UI
{
    // Tüm ekranların temel sınıfı
    public abstract class GameScreen : MonoBehaviour
    {
        protected GameManager G { get { return GameManager.I; } }
        protected GameSession Session { get { return GameManager.I.Session; } }
        protected PlayerState State { get { return GameManager.I.State; } }
        public RectTransform Root { get { return (RectTransform)transform; } }

        public abstract void Build();
        public virtual void Refresh() { }
        public virtual void OnShow() { Refresh(); }
    }
}
