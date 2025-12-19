using UnityEngine;

namespace TowerOffense.Saving
{
    public class JsonSaveSerializer
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }

        public string Serialize(RunSnapshot snapshot)
        {
            Debug.Log("Serializing snapshot.");
            return string.Empty;
        }

        public RunSnapshot Deserialize(string json)
        {
            Debug.Log("Deserializing snapshot.");
            return new RunSnapshot();
        }

    }
}
