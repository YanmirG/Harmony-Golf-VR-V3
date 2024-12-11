using UnityEngine;

namespace Convai.Scripts.Runtime.Extensions
{
    public static class GameObjectExtension
    {
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            return gameObject.TryGetComponent(out T t) ? t : gameObject.AddComponent<T>();
        }
    }
}