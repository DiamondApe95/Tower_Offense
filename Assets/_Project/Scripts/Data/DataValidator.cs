using UnityEngine;

namespace TowerOffense.Data
{
    public class DataValidator
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public bool Validate()
        {
            Debug.Log("Validating data.");
            return true;
        }

    }
}
