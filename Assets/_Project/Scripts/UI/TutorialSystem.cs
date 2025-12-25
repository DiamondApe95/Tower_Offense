using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TowerConquest.Core;
using TowerConquest.Saving;

namespace TowerConquest.UI
{
    /// <summary>
    /// TutorialSystem: Verwaltet In-Game-Tutorials und Hinweise.
    /// Zeigt kontextuelle Hilfe für neue Spieler.
    /// </summary>
    public class TutorialSystem : MonoBehaviour
    {
        public static TutorialSystem Instance { get; private set; }

        [Header("UI References")]
        public GameObject tutorialPanel;
        public Text titleText;
        public Text descriptionText;
        public Image tutorialImage;
        public Button nextButton;
        public Button skipButton;
        public Button closeButton;
        public Text stepIndicatorText;

        [Header("Highlight")]
        public GameObject highlightPrefab;
        public RectTransform highlightTarget;
        public Image highlightOverlay;
        public Color highlightColor = new Color(1f, 0.8f, 0.2f, 0.5f);

        [Header("Arrow Indicator")]
        public RectTransform arrowIndicator;
        public float arrowBobSpeed = 2f;
        public float arrowBobAmount = 10f;

        [Header("Settings")]
        public bool showOnFirstPlay = true;
        public float stepDelay = 0.5f;
        public bool pauseGameDuringTutorial = true;

        [Header("Tutorial Steps")]
        public List<TutorialStep> tutorialSteps = new List<TutorialStep>();

        public event Action OnTutorialStarted;
        public event Action OnTutorialCompleted;
        public event Action<int> OnStepChanged;

        private int currentStepIndex = -1;
        private bool isTutorialActive;
        private float previousTimeScale;
        private Coroutine highlightCoroutine;
        private GameObject currentHighlight;

        [Serializable]
        public class TutorialStep
        {
            public string id;
            public string title;
            [TextArea(2, 5)]
            public string description;
            public Sprite image;
            public string targetObjectName;
            public Vector2 panelOffset;
            public bool requiresAction;
            public string completionTrigger;
            public float autoAdvanceDelay = 0f;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            SetupButtons();
            HideTutorial();

            if (showOnFirstPlay && ShouldShowTutorial())
            {
                StartCoroutine(DelayedStart());
            }
        }

        private IEnumerator DelayedStart()
        {
            yield return new WaitForSeconds(1f);
            StartTutorial();
        }

        private void Update()
        {
            // Arrow bobbing
            if (arrowIndicator != null && arrowIndicator.gameObject.activeSelf)
            {
                float bob = Mathf.Sin(Time.unscaledTime * arrowBobSpeed) * arrowBobAmount;
                arrowIndicator.anchoredPosition = new Vector2(arrowIndicator.anchoredPosition.x, bob);
            }
        }

        private void SetupButtons()
        {
            if (nextButton != null)
                nextButton.onClick.AddListener(NextStep);

            if (skipButton != null)
                skipButton.onClick.AddListener(SkipTutorial);

            if (closeButton != null)
                closeButton.onClick.AddListener(CloseTutorial);
        }

        private bool ShouldShowTutorial()
        {
            GameSettings settings = GameSettings.Load();
            if (settings.skipTutorials) return false;

            if (ServiceLocator.TryGet(out SaveManager saveManager))
            {
                PlayerProgress progress = saveManager.GetOrCreateProgress();
                return !progress.tutorialCompleted;
            }

            return true;
        }

        public void StartTutorial()
        {
            if (tutorialSteps.Count == 0)
            {
                Debug.LogWarning("TutorialSystem: No tutorial steps defined.");
                return;
            }

            isTutorialActive = true;
            currentStepIndex = -1;

            if (pauseGameDuringTutorial)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }

            OnTutorialStarted?.Invoke();
            NextStep();
        }

        public void NextStep()
        {
            currentStepIndex++;

            if (currentStepIndex >= tutorialSteps.Count)
            {
                CompleteTutorial();
                return;
            }

            ShowStep(tutorialSteps[currentStepIndex]);
            OnStepChanged?.Invoke(currentStepIndex);
        }

        public void PreviousStep()
        {
            if (currentStepIndex > 0)
            {
                currentStepIndex--;
                ShowStep(tutorialSteps[currentStepIndex]);
                OnStepChanged?.Invoke(currentStepIndex);
            }
        }

        private void ShowStep(TutorialStep step)
        {
            if (tutorialPanel == null) return;

            tutorialPanel.SetActive(true);

            if (titleText != null)
                titleText.text = step.title;

            if (descriptionText != null)
                descriptionText.text = step.description;

            if (tutorialImage != null)
            {
                tutorialImage.sprite = step.image;
                tutorialImage.gameObject.SetActive(step.image != null);
            }

            if (stepIndicatorText != null)
                stepIndicatorText.text = $"{currentStepIndex + 1} / {tutorialSteps.Count}";

            // Highlight target
            if (!string.IsNullOrEmpty(step.targetObjectName))
            {
                HighlightTarget(step.targetObjectName);
            }
            else
            {
                ClearHighlight();
            }

            // Panel position
            if (step.panelOffset != Vector2.zero)
            {
                RectTransform panelRect = tutorialPanel.GetComponent<RectTransform>();
                if (panelRect != null)
                {
                    panelRect.anchoredPosition = step.panelOffset;
                }
            }

            // Auto-advance
            if (step.autoAdvanceDelay > 0 && !step.requiresAction)
            {
                StartCoroutine(AutoAdvance(step.autoAdvanceDelay));
            }

            // Button visibility
            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(!step.requiresAction);
            }
        }

        private IEnumerator AutoAdvance(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            NextStep();
        }

        private void HighlightTarget(string targetName)
        {
            ClearHighlight();

            GameObject target = GameObject.Find(targetName);
            if (target == null)
            {
                Debug.LogWarning($"TutorialSystem: Target '{targetName}' not found.");
                return;
            }

            // UI Element highlight
            RectTransform targetRect = target.GetComponent<RectTransform>();
            if (targetRect != null && highlightPrefab != null)
            {
                currentHighlight = Instantiate(highlightPrefab, targetRect.parent);
                RectTransform highlightRect = currentHighlight.GetComponent<RectTransform>();
                if (highlightRect != null)
                {
                    highlightRect.anchoredPosition = targetRect.anchoredPosition;
                    highlightRect.sizeDelta = targetRect.sizeDelta + new Vector2(20, 20);
                }

                // Pulsing animation
                highlightCoroutine = StartCoroutine(PulseHighlight(currentHighlight));
            }

            // Arrow pointing
            if (arrowIndicator != null && targetRect != null)
            {
                arrowIndicator.gameObject.SetActive(true);
                arrowIndicator.position = targetRect.position + new Vector3(0, targetRect.rect.height / 2 + 30, 0);
            }
        }

        private IEnumerator PulseHighlight(GameObject highlight)
        {
            Image img = highlight.GetComponent<Image>();
            if (img == null) yield break;

            while (highlight != null)
            {
                float alpha = 0.3f + 0.2f * Mathf.Sin(Time.unscaledTime * 3f);
                Color c = highlightColor;
                c.a = alpha;
                img.color = c;
                yield return null;
            }
        }

        private void ClearHighlight()
        {
            if (highlightCoroutine != null)
            {
                StopCoroutine(highlightCoroutine);
                highlightCoroutine = null;
            }

            if (currentHighlight != null)
            {
                Destroy(currentHighlight);
                currentHighlight = null;
            }

            if (arrowIndicator != null)
            {
                arrowIndicator.gameObject.SetActive(false);
            }
        }

        public void TriggerCompletion(string triggerId)
        {
            if (!isTutorialActive) return;
            if (currentStepIndex < 0 || currentStepIndex >= tutorialSteps.Count) return;

            TutorialStep currentStep = tutorialSteps[currentStepIndex];
            if (currentStep.requiresAction && currentStep.completionTrigger == triggerId)
            {
                NextStep();
            }
        }

        public void SkipTutorial()
        {
            CompleteTutorial();
        }

        public void CloseTutorial()
        {
            HideTutorial();
            RestoreTimeScale();
        }

        private void CompleteTutorial()
        {
            isTutorialActive = false;
            HideTutorial();
            RestoreTimeScale();

            // Mark as completed
            if (ServiceLocator.TryGet(out SaveManager saveManager))
            {
                PlayerProgress progress = saveManager.GetOrCreateProgress();
                progress.tutorialCompleted = true;
                saveManager.SaveProgress(progress);
            }

            OnTutorialCompleted?.Invoke();
            Debug.Log("TutorialSystem: Tutorial completed.");
        }

        private void HideTutorial()
        {
            ClearHighlight();

            if (tutorialPanel != null)
            {
                tutorialPanel.SetActive(false);
            }
        }

        private void RestoreTimeScale()
        {
            if (pauseGameDuringTutorial)
            {
                Time.timeScale = previousTimeScale > 0 ? previousTimeScale : 1f;
            }
        }

        public bool IsTutorialActive => isTutorialActive;
        public int CurrentStepIndex => currentStepIndex;
        public int TotalSteps => tutorialSteps.Count;

        /// <summary>
        /// Zeigt einen einzelnen Hinweis ohne vollständiges Tutorial.
        /// </summary>
        public void ShowHint(string title, string description, float duration = 5f)
        {
            StartCoroutine(ShowHintCoroutine(title, description, duration));
        }

        private IEnumerator ShowHintCoroutine(string title, string description, float duration)
        {
            if (tutorialPanel == null) yield break;

            tutorialPanel.SetActive(true);

            if (titleText != null)
                titleText.text = title;

            if (descriptionText != null)
                descriptionText.text = description;

            if (tutorialImage != null)
                tutorialImage.gameObject.SetActive(false);

            if (stepIndicatorText != null)
                stepIndicatorText.gameObject.SetActive(false);

            if (nextButton != null)
                nextButton.gameObject.SetActive(false);

            if (skipButton != null)
                skipButton.gameObject.SetActive(false);

            yield return new WaitForSecondsRealtime(duration);

            if (!isTutorialActive)
            {
                HideTutorial();
            }
        }

        /// <summary>
        /// Initialisiert Standard-Tutorial-Schritte für Offense-Modus.
        /// </summary>
        public void SetupOffenseTutorial()
        {
            tutorialSteps.Clear();

            tutorialSteps.Add(new TutorialStep
            {
                id = "welcome",
                title = "Willkommen!",
                description = "In Tower Conquest führst du deine Armee zum Sieg. Zerstöre die feindliche Basis, um zu gewinnen!",
                autoAdvanceDelay = 0f
            });

            tutorialSteps.Add(new TutorialStep
            {
                id = "cards",
                title = "Deine Karten",
                description = "Unten siehst du deine Handkarten. Jede Karte beschwört eine Einheit oder wirkt einen Zauber.",
                targetObjectName = "HandContainer",
                autoAdvanceDelay = 0f
            });

            tutorialSteps.Add(new TutorialStep
            {
                id = "energy",
                title = "Energie",
                description = "Karten kosten Energie. Deine Energie regeneriert sich zu Beginn jeder Welle.",
                targetObjectName = "EnergyText",
                autoAdvanceDelay = 0f
            });

            tutorialSteps.Add(new TutorialStep
            {
                id = "play_card",
                title = "Karte spielen",
                description = "Klicke auf eine Karte, um sie zu spielen. Deine Einheiten erscheinen am Startpunkt.",
                requiresAction = true,
                completionTrigger = "card_played"
            });

            tutorialSteps.Add(new TutorialStep
            {
                id = "start_wave",
                title = "Welle starten",
                description = "Wenn du bereit bist, klicke 'Start Wave' um die Welle zu beginnen!",
                targetObjectName = "StartWaveButton",
                requiresAction = true,
                completionTrigger = "wave_started"
            });

            tutorialSteps.Add(new TutorialStep
            {
                id = "victory",
                title = "Viel Erfolg!",
                description = "Zerstöre die feindliche Basis bevor alle Wellen vorbei sind. Viel Glück!",
                autoAdvanceDelay = 3f
            });
        }

        /// <summary>
        /// Initialisiert Standard-Tutorial-Schritte für Defense-Modus.
        /// </summary>
        public void SetupDefenseTutorial()
        {
            tutorialSteps.Clear();

            tutorialSteps.Add(new TutorialStep
            {
                id = "welcome_defense",
                title = "Defense Modus",
                description = "Verteidige deine Basis gegen angreifende Feinde, indem du Türme baust!",
                autoAdvanceDelay = 0f
            });

            tutorialSteps.Add(new TutorialStep
            {
                id = "gold",
                title = "Gold",
                description = "Du hast Gold zum Bauen von Türmen. Besiege Feinde für mehr Gold!",
                targetObjectName = "GoldText",
                autoAdvanceDelay = 0f
            });

            tutorialSteps.Add(new TutorialStep
            {
                id = "build",
                title = "Turm bauen",
                description = "Wähle einen Turm-Typ und klicke auf ein Baufeld, um ihn zu platzieren.",
                requiresAction = true,
                completionTrigger = "tower_built"
            });

            tutorialSteps.Add(new TutorialStep
            {
                id = "start_defense",
                title = "Bereit?",
                description = "Starte die Welle, wenn du genug Türme gebaut hast!",
                autoAdvanceDelay = 0f
            });
        }
    }
}
