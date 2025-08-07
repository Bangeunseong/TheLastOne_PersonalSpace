using System;
using System.Collections.Generic;
using UnityEngine;

namespace _1.Scripts.Quests.Core
{
    public class GameEventSystem : MonoBehaviour
    {
        private static GameEventSystem instance;
        public static GameEventSystem Instance => instance ??= FindObjectOfType<GameEventSystem>();

        private readonly LinkedList<IGameEventListener> listeners = new();

        private void OnDestroy()
        {
            listeners.Clear();
            instance = null;
        }

        public void RegisterListener(IGameEventListener listener)
        {
            if (!listeners.Contains(listener))
                listeners.AddLast(listener);
        }

        public void UnregisterListener(IGameEventListener listener)
        {
            if (listeners.Contains(listener))
                listeners.Remove(listener);
        }

        public void RaiseEvent(int eventID) 
        {
            var node = listeners.First;
            while (node != null)
            {
                var current = node;
                node = node.Next;
                current.Value.OnEventRaised(eventID);
            }
        }
    }
}