using System;
using System.Collections.Generic;
using UnityEngine;

namespace TowerConquest.Core
{
    /// <summary>
    /// EventBus: Zentrales Event-System für lose gekoppelte Kommunikation zwischen Komponenten.
    /// Unterstützt typisierte Events und Subscriptions.
    /// </summary>
    public class EventBus
    {
        private readonly Dictionary<Type, List<Delegate>> subscribers = new();
        private readonly Dictionary<string, List<Action<object>>> namedSubscribers = new();
        private readonly Queue<Action> pendingEvents = new();
        private bool isProcessing;

        /// <summary>
        /// Veröffentlicht ein typisiertes Event.
        /// </summary>
        public void Publish<T>(T eventData)
        {
            Type eventType = typeof(T);

            if (!subscribers.TryGetValue(eventType, out List<Delegate> handlers))
            {
                return;
            }

            foreach (var handler in handlers.ToArray())
            {
                try
                {
                    ((Action<T>)handler)?.Invoke(eventData);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"EventBus: Error in handler for {eventType.Name}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Veröffentlicht ein benanntes Event.
        /// </summary>
        public void Publish(string eventName, object data = null)
        {
            if (!namedSubscribers.TryGetValue(eventName, out List<Action<object>> handlers))
            {
                return;
            }

            foreach (var handler in handlers.ToArray())
            {
                try
                {
                    handler?.Invoke(data);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"EventBus: Error in handler for '{eventName}': {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Veröffentlicht ein Event verzögert im nächsten Frame.
        /// </summary>
        public void PublishDelayed<T>(T eventData)
        {
            pendingEvents.Enqueue(() => Publish(eventData));
        }

        /// <summary>
        /// Verarbeitet alle verzögerten Events.
        /// </summary>
        public void ProcessPendingEvents()
        {
            if (isProcessing) return;

            isProcessing = true;
            while (pendingEvents.Count > 0)
            {
                var action = pendingEvents.Dequeue();
                action?.Invoke();
            }
            isProcessing = false;
        }

        /// <summary>
        /// Abonniert ein typisiertes Event.
        /// </summary>
        public void Subscribe<T>(Action<T> handler)
        {
            if (handler == null) return;

            Type eventType = typeof(T);

            if (!subscribers.ContainsKey(eventType))
            {
                subscribers[eventType] = new List<Delegate>();
            }

            if (!subscribers[eventType].Contains(handler))
            {
                subscribers[eventType].Add(handler);
            }
        }

        /// <summary>
        /// Abonniert ein benanntes Event.
        /// </summary>
        public void Subscribe(string eventName, Action<object> handler)
        {
            if (handler == null || string.IsNullOrEmpty(eventName)) return;

            if (!namedSubscribers.ContainsKey(eventName))
            {
                namedSubscribers[eventName] = new List<Action<object>>();
            }

            if (!namedSubscribers[eventName].Contains(handler))
            {
                namedSubscribers[eventName].Add(handler);
            }
        }

        /// <summary>
        /// Deabonniert ein typisiertes Event.
        /// </summary>
        public void Unsubscribe<T>(Action<T> handler)
        {
            if (handler == null) return;

            Type eventType = typeof(T);

            if (subscribers.TryGetValue(eventType, out List<Delegate> handlers))
            {
                handlers.Remove(handler);
            }
        }

        /// <summary>
        /// Deabonniert ein benanntes Event.
        /// </summary>
        public void Unsubscribe(string eventName, Action<object> handler)
        {
            if (handler == null || string.IsNullOrEmpty(eventName)) return;

            if (namedSubscribers.TryGetValue(eventName, out List<Action<object>> handlers))
            {
                handlers.Remove(handler);
            }
        }

        /// <summary>
        /// Entfernt alle Subscriptions für einen Event-Typ.
        /// </summary>
        public void ClearSubscriptions<T>()
        {
            Type eventType = typeof(T);
            if (subscribers.ContainsKey(eventType))
            {
                subscribers[eventType].Clear();
            }
        }

        /// <summary>
        /// Entfernt alle Subscriptions für ein benanntes Event.
        /// </summary>
        public void ClearSubscriptions(string eventName)
        {
            if (namedSubscribers.ContainsKey(eventName))
            {
                namedSubscribers[eventName].Clear();
            }
        }

        /// <summary>
        /// Entfernt alle Subscriptions.
        /// </summary>
        public void ClearAllSubscriptions()
        {
            subscribers.Clear();
            namedSubscribers.Clear();
            pendingEvents.Clear();
        }

        /// <summary>
        /// Gibt die Anzahl der Subscriber für einen Event-Typ zurück.
        /// </summary>
        public int GetSubscriberCount<T>()
        {
            Type eventType = typeof(T);
            return subscribers.TryGetValue(eventType, out List<Delegate> handlers) ? handlers.Count : 0;
        }
    }

    // =====================
    // GAME EVENTS
    // =====================

    /// <summary>
    /// Event für Spielpause-Zustandsänderungen.
    /// </summary>
    public struct GamePausedEvent
    {
        public bool IsPaused { get; }

        public GamePausedEvent(bool isPaused)
        {
            IsPaused = isPaused;
        }
    }

    /// <summary>
    /// Event wenn eine Welle startet.
    /// </summary>
    public struct WaveStartedEvent
    {
        public int WaveIndex { get; }
        public int TotalWaves { get; }

        public WaveStartedEvent(int waveIndex, int totalWaves)
        {
            WaveIndex = waveIndex;
            TotalWaves = totalWaves;
        }
    }

    /// <summary>
    /// Event wenn eine Welle endet.
    /// </summary>
    public struct WaveEndedEvent
    {
        public int WaveIndex { get; }
        public bool IsVictory { get; }

        public WaveEndedEvent(int waveIndex, bool isVictory)
        {
            WaveIndex = waveIndex;
            IsVictory = isVictory;
        }
    }

    /// <summary>
    /// Event wenn eine Einheit spawnt.
    /// </summary>
    public struct UnitSpawnedEvent
    {
        public string UnitId { get; }
        public Vector3 Position { get; }

        public UnitSpawnedEvent(string unitId, Vector3 position)
        {
            UnitId = unitId;
            Position = position;
        }
    }

    /// <summary>
    /// Event wenn eine Einheit stirbt.
    /// </summary>
    public struct UnitDiedEvent
    {
        public string UnitId { get; }
        public GameObject Source { get; }

        public UnitDiedEvent(string unitId, GameObject source)
        {
            UnitId = unitId;
            Source = source;
        }
    }

    /// <summary>
    /// Event wenn die Basis Schaden nimmt.
    /// </summary>
    public struct BaseDamagedEvent
    {
        public float Damage { get; }
        public float CurrentHp { get; }
        public float MaxHp { get; }

        public BaseDamagedEvent(float damage, float currentHp, float maxHp)
        {
            Damage = damage;
            CurrentHp = currentHp;
            MaxHp = maxHp;
        }
    }

    /// <summary>
    /// Event wenn eine Karte gespielt wird.
    /// </summary>
    public struct CardPlayedEvent
    {
        public string CardId { get; }
        public int EnergyCost { get; }

        public CardPlayedEvent(string cardId, int energyCost)
        {
            CardId = cardId;
            EnergyCost = energyCost;
        }
    }

    /// <summary>
    /// Event wenn ein Level abgeschlossen wird.
    /// </summary>
    public struct LevelCompletedEvent
    {
        public string LevelId { get; }
        public bool IsVictory { get; }
        public int Score { get; }
        public int Stars { get; }

        public LevelCompletedEvent(string levelId, bool isVictory, int score, int stars)
        {
            LevelId = levelId;
            IsVictory = isVictory;
            Score = score;
            Stars = stars;
        }
    }

    /// <summary>
    /// Event für Energie-Änderungen.
    /// </summary>
    public struct EnergyChangedEvent
    {
        public int CurrentEnergy { get; }
        public int MaxEnergy { get; }
        public int Change { get; }

        public EnergyChangedEvent(int currentEnergy, int maxEnergy, int change)
        {
            CurrentEnergy = currentEnergy;
            MaxEnergy = maxEnergy;
            Change = change;
        }
    }

    /// <summary>
    /// Event für Gold-Änderungen.
    /// </summary>
    public struct GoldChangedEvent
    {
        public int CurrentGold { get; }
        public int Change { get; }

        public GoldChangedEvent(int currentGold, int change)
        {
            CurrentGold = currentGold;
            Change = change;
        }
    }
}
