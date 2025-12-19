using TowerOffense.Data;
using TowerOffense.Saving;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TowerOffense.Core
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

            Debug.Log($"Loaded Units: {db.Units.Count}, Spells: {db.Spells.Count}, Towers: {db.Towers.Count}, Traps: {db.Traps.Count}, Levels: {db.Levels.Count}.");

            LoadMainMenuIfAvailable();
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
