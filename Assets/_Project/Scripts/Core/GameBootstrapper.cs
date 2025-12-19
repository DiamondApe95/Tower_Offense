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

            InitializeServices();
            LoadInitialScene();
        }

        private void InitializeServices()
        {
            ServiceLocator.Register(new EventBus());
            ServiceLocator.Register(new GameTime());
            ServiceLocator.Register(new SaveManager());

            var database = new JsonDatabase();
            database.LoadAll();
            ServiceLocator.Register(database);
        }

        private void LoadInitialScene()
        {
            if (Application.CanStreamedLevelBeLoaded("MainMenu"))
            {
                SceneManager.LoadScene("MainMenu");
            }
        }
    }
}
