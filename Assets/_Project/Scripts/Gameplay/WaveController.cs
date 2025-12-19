using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerConquest.Gameplay
{
    public class WaveController : MonoBehaviour
    {
        public float spawnIntervalSeconds = 0.4f;

        public void StartWave(LevelController ctx)
        {
            if (ctx == null || ctx.Run == null)
            {
                Debug.LogWarning("WaveController.StartWave called without context.");
                return;
            }

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

            StartCoroutine(RunWave(ctx));
        }

        private IEnumerator RunWave(LevelController ctx)
        {
            IReadOnlyList<string> unitsToSpawn = ctx.GetWaveUnits();
            if (unitsToSpawn == null || unitsToSpawn.Count == 0)
            {
                Debug.LogWarning("WaveController: No units configured for this wave.");
            }
            else
            {
                foreach (string unitId in unitsToSpawn)
                {
                    if (ctx.Spawner == null)
                    {
                        Debug.LogWarning("WaveController: SpawnController missing.");
                        break;
                    }

                    ctx.Spawner.SpawnUnitGroup(unitId);
                    yield return new WaitForSeconds(spawnIntervalSeconds);
                }
            }

            while (!ctx.IsBaseDestroyed() && ctx.HasActiveUnits())
            {
                yield return new WaitForSeconds(0.5f);
            }

            ctx.OnWaveFinished();
        }
    }
}
