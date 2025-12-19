using UnityEngine;
using TowerOffense.Gameplay.Entities;

namespace TowerOffense.Combat
{
    public class SlowStatus : MonoBehaviour
    {
        private float duration;
        private float originalMultiplier = 1f;

        public void Initialize(float slowPercent, float durationSeconds)
        {
            duration = durationSeconds;

            UnitMover mover = GetComponent<UnitMover>();
            if (mover != null)
            {
                originalMultiplier = mover.moveSpeedMultiplier;
                mover.moveSpeedMultiplier = Mathf.Clamp01(1f - slowPercent);
            }
        }

        private void Update()
        {
            if (duration <= 0f)
            {
                Destroy(this);
                return;
            }

            duration -= Time.deltaTime;
        }

        private void OnDestroy()
        {
            UnitMover mover = GetComponent<UnitMover>();
            if (mover != null)
            {
                mover.moveSpeedMultiplier = originalMultiplier;
            }
        }
    }
}
