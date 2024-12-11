using System;
using System.Collections.Generic;
using UnityEngine;

namespace Convai.Scripts.Runtime.Utils
{
    public class MainThreadDispatcher : MonoBehaviour
    {
        private readonly Queue<Action> _actions = new();

        public static MainThreadDispatcher Instance { get; private set; }


        private void Update()
        {
            lock (_actions)
            {
                while (_actions.Count > 0)
                {
                    Action actionToInvoke = _actions.Dequeue();
                    actionToInvoke?.Invoke();
                }
            }
        }

        public static void CreateInstance()
        {
            Instance = new GameObject("MainThreadDispatcher").AddComponent<MainThreadDispatcher>();
            DontDestroyOnLoad(Instance.gameObject);
        }

        public void RunOnMainThread(Action action)
        {
            lock (_actions)
            {
                _actions.Enqueue(action);
            }
        }
    }
}