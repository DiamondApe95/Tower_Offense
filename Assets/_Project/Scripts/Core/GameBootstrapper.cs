using TowerConquest.Data;
using TowerConquest.Saving;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TowerConquest.Core
{
    public class GameBootstrapper : MonoBehaviour
    {
        private static GameBootstrapper instance;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }

        private void Initialize()
        {
            ServiceLocator.Register(new EventBus());
            ServiceLocator.Register(new GameTime());
            ServiceLocator.Register(new SaveManager());

            var db = new JsonDatabase();
            db.LoadAll();
            DataValidator.ValidateAll(db);
            ServiceLocator.Register(db);

            UnityEngine.Debug.Log($"Loaded Units: {db.Units.Count}, Spells: {db.Spells.Count}, Towers: {db.Towers.Count}, Traps: {db.Traps.Count}, Levels: {db.Levels.Count}, Heroes: {db.Heroes.Count}.");

            RegisterPrefabRegistry();

            LoadMainMenuIfAvailable();
        }

        private void RegisterPrefabRegistry()
        {
            PrefabRegistry registry = FindObjectOfType<PrefabRegistry>();
            if (registry != null)
            {
                ServiceLocator.Register(registry);
                UnityEngine.Debug.Log("PrefabRegistry registered successfully.");
            }
            else
            {
                UnityEngine.Debug.LogWarning("PrefabRegistry not found in scene. Prefabs will use fallback primitives.");
            }
        }

        private static void LoadMainMenuIfAvailable()
        {
            if (SceneManager.GetActiveScene().name == "MainMenu")
            {
                return;
            }

            if (Application.CanStreamedLevelBeLoaded("MainMenu"))
            {
                SceneManager.LoadScene("MainMenu");
            }
        }
    }
}
