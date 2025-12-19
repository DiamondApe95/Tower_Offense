using System.Collections;
using UnityEngine;

namespace TowerOffense.Gameplay
{
    public class WaveController : MonoBehaviour
    {
        public void StartWave(LevelController ctx)
        {
            if (ctx != null && ctx.Run != null)
            {
                RunState run = ctx.Run;
                run.heroAvailableThisWave = run.heroEveryNWaves > 0 && run.waveIndex % run.heroEveryNWaves == 0;
                if (run.heroAvailableThisWave)
                {
                    if (string.IsNullOrWhiteSpace(run.selectedHeroId))
                    {
                        run.selectedHeroId = "hero_legatus";
                    }

                    ctx.SpawnHero(run.selectedHeroId);
                }
            }

            StartCoroutine(SimulateWave(ctx));
        }

        private IEnumerator SimulateWave(LevelController ctx)
        {
            yield return new WaitForSeconds(2f);
            ctx.OnWaveSimulatedEnd();
        }
    }
}
