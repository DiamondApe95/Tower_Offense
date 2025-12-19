using UnityEngine;

namespace TowerOffense.Gameplay
{
    public class LevelStateMachine
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public void ChangeState(RunState newState)
        {
            Debug.Log("Stub method called.");
        }

    }
}
