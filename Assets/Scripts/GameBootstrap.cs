using UnityEngine;

namespace Shiro
{
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateGame()
        {
#if UNITY_2023_1_OR_NEWER
            if (Object.FindAnyObjectByType<GameController>() != null)
#else
            if (Object.FindObjectOfType<GameController>() != null)
#endif
            {
                return;
            }

            var root = new GameObject("Shiro Prototype");
            root.AddComponent<GameController>();
        }
    }
}
