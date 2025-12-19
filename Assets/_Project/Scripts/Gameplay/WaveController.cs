using System.Collections;
using UnityEngine;

namespace TowerOffense.Gameplay
{
    public class WaveController : MonoBehaviour
    {
        public void StartWave(LevelController ctx)
        {
            StartCoroutine(SimulateWave(ctx));
        }

        private IEnumerator SimulateWave(LevelController ctx)
        {
            yield return new WaitForSeconds(2f);
            ctx.OnWaveSimulatedEnd();
        }
    }
}
