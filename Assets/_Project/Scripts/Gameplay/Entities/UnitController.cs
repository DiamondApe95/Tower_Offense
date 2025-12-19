using System.Collections.Generic;
using UnityEngine;

namespace TowerOffense.Gameplay.Entities
{
    public class UnitController : MonoBehaviour
    {
        public string UnitId { get; private set; }

        public void Initialize(string unitId, IReadOnlyList<Vector3> path)
        {
            UnitId = unitId;
            UnitMover mover = GetComponent<UnitMover>();
            if (mover == null)
            {
                mover = gameObject.AddComponent<UnitMover>();
            }

            mover.Initialize(path);
        }
    }
}
