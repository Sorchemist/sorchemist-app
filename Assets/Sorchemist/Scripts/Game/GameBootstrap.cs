// Play'e basınca oyunu otomatik başlatır. Sahneye hiçbir şey eklemen gerekmez.
using UnityEngine;

namespace Sorchemist.Game
{
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Object.FindFirstObjectByType<GameManager>() != null) return;
            new GameObject("Sorchemist").AddComponent<GameManager>();
        }
    }
}
