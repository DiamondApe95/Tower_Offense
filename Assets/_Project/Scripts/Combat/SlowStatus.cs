using UnityEngine;
using TowerConquest.Gameplay.Entities;

namespace TowerConquest.Combat
{
    public class SlowStatus : MonoBehaviour
    {
        private float duration;
        private float originalMultiplier = 1f;
        private float slowPercent;

        public void Initialize(float slowPercent, float durationSeconds)
        {
            duration = durationSeconds;
            this.slowPercent = Mathf.Clamp01(slowPercent);

            UnitMover mover = GetComponent<UnitMover>();
            if (mover != null)
            {
                originalMultiplier = mover.moveSpeedMultiplier;
                mover.moveSpeedMultiplier = Mathf.Clamp01(1f - this.slowPercent);
            }
        }

        public void Refresh(float newSlowPercent, float durationSeconds)
        {
            float clamped = Mathf.Clamp01(newSlowPercent);
            if (clamped > slowPercent)
            {
                slowPercent = clamped;
                UnitMover mover = GetComponent<UnitMover>();
                if (mover != null)
                {
                    mover.moveSpeedMultiplier = Mathf.Clamp01(1f - slowPercent);
                }
            }

            duration = Mathf.Max(duration, durationSeconds);
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
