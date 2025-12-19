using UnityEngine;
using UnityEngine.UI;

namespace TowerConquest.UI
{
    public class ResultScreenView : MonoBehaviour
    {
        public GameObject root;
        public Text resultLabel;
        public Button nextLevelButton;

        public void ShowResults(bool victory, bool nextLevelUnlocked)
        {
            if (root != null)
            {
                root.SetActive(true);
            }

            if (resultLabel != null)
            {
                resultLabel.text = victory ? "VICTORY" : "DEFEAT";
            }

            if (nextLevelButton != null)
            {
                nextLevelButton.gameObject.SetActive(nextLevelUnlocked);
                nextLevelButton.interactable = nextLevelUnlocked;
            }

            UnityEngine.Debug.Log($"ResultScreenView: Showing results. Victory={victory}, NextLevelUnlocked={nextLevelUnlocked}.");
        }
    }
}
