using UnityEngine;

namespace TowerOffense.Gameplay
{
    public class RunState
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public void Enter()
        {
            UnityEngine.Debug.Log("Stub method called.");
        }

        public void Exit()
        {
            UnityEngine.Debug.Log("Stub method called.");
        }

    }
}
