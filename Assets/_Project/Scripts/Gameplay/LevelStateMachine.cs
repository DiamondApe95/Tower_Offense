using UnityEngine;

namespace TowerConquest.Gameplay
{
    public class LevelStateMachine
    {
        private enum State
        {
            Planning,
            Attacking,
            Results
        }

        private State state;

        public event System.Action OnPlanningStarted;
        public event System.Action<int> OnWaveStarted;
        public event System.Action<int> OnWaveEnded;
        public event System.Action<bool> OnFinished;

        public void EnterPlanning(RunState run)
        {
            state = State.Planning;
            run.isPlanning = true;
            run.isAttacking = false;
            run.isFinished = false;
            run.isVictory = false;
            run.energy = run.maxEnergyPerWave;
            UnityEngine.Debug.Log($"LevelStateMachine: Entered planning for level {run.levelId}.");
            OnPlanningStarted?.Invoke();
        }

        public void StartWave(RunState run)
        {
            if (state != State.Planning)
            {
                UnityEngine.Debug.LogWarning("LevelStateMachine: StartWave called outside planning state.");
                return;
            }

            state = State.Attacking;
            run.isPlanning = false;
            run.isAttacking = true;
            run.waveIndex += 1;
            UnityEngine.Debug.Log($"LevelStateMachine: Wave {run.waveIndex} started.");
            OnWaveStarted?.Invoke(run.waveIndex);
        }

        public void EndWave(RunState run)
        {
            state = State.Planning;
            run.isPlanning = true;
            run.isAttacking = false;
            run.energy = run.maxEnergyPerWave;
            UnityEngine.Debug.Log($"LevelStateMachine: Wave {run.waveIndex} ended.");
            OnWaveEnded?.Invoke(run.waveIndex);
            OnPlanningStarted?.Invoke();
        }

        public void Finish(RunState run, bool victory)
        {
            state = State.Results;
            run.isFinished = true;
            run.isVictory = victory;
            run.isPlanning = false;
            run.isAttacking = false;
            UnityEngine.Debug.Log($"LevelStateMachine: Finished. Victory={victory}.");
            OnFinished?.Invoke(victory);
        }
    }
}
