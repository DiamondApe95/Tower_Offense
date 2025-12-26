using System;
using TowerConquest.Debug;
using System.Collections.Generic;
using UnityEngine;
using TowerConquest.Core;

namespace TowerConquest.Audio
{
    /// <summary>
    /// AudioManager: Zentrales Audiosystem für Musik und Soundeffekte.
    /// Unterstützt Pools, Lautstärkeregelung und Audio-Events.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        public AudioSource musicSource;
        public AudioSource ambientSource;
        public int sfxPoolSize = 10;

        [Header("Volume Settings")]
        [Range(0f, 1f)] public float masterVolume = 1f;
        [Range(0f, 1f)] public float musicVolume = 0.7f;
        [Range(0f, 1f)] public float sfxVolume = 1f;
        [Range(0f, 1f)] public float ambientVolume = 0.5f;

        [Header("Audio Clips")]
        public AudioClip menuMusic;
        public AudioClip gameplayMusic;
        public AudioClip victoryMusic;
        public AudioClip defeatMusic;

        [Header("SFX Library")]
        public List<SfxEntry> sfxLibrary = new List<SfxEntry>();

        [Header("Settings")]
        public float musicFadeDuration = 1f;
        public bool playMusicOnStart = true;

        private AudioSource[] sfxPool;
        private int sfxPoolIndex;
        private Dictionary<string, AudioClip> sfxDictionary;
        private Coroutine musicFadeCoroutine;
        private bool isMuted;

        [Serializable]
        public class SfxEntry
        {
            public string id;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 1f;
            [Range(0.5f, 1.5f)] public float pitchVariation = 0f;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeSfxPool();
            BuildSfxDictionary();
            LoadVolumeSettings();
        }

        private void Start()
        {
            if (playMusicOnStart && menuMusic != null)
            {
                PlayMusic(menuMusic);
            }

            // Subscribe to game events
            if (ServiceLocator.TryGet(out EventBus eventBus))
            {
                eventBus.Subscribe<GamePausedEvent>(OnGamePaused);
            }
        }

        private void OnDestroy()
        {
            if (ServiceLocator.TryGet(out EventBus eventBus))
            {
                eventBus.Unsubscribe<GamePausedEvent>(OnGamePaused);
            }
        }

        private void InitializeSfxPool()
        {
            sfxPool = new AudioSource[sfxPoolSize];
            for (int i = 0; i < sfxPoolSize; i++)
            {
                GameObject sfxObject = new GameObject($"SfxSource_{i}");
                sfxObject.transform.SetParent(transform);
                sfxPool[i] = sfxObject.AddComponent<AudioSource>();
                sfxPool[i].playOnAwake = false;
            }
        }

        private void BuildSfxDictionary()
        {
            sfxDictionary = new Dictionary<string, AudioClip>();
            foreach (var entry in sfxLibrary)
            {
                if (!string.IsNullOrEmpty(entry.id) && entry.clip != null)
                {
                    sfxDictionary[entry.id] = entry.clip;
                }
            }
        }

        private void LoadVolumeSettings()
        {
            GameSettings settings = GameSettings.Load();
            masterVolume = settings.masterVolume;
            musicVolume = settings.musicVolume;
            sfxVolume = settings.sfxVolume;
            UpdateVolumes();
        }

        public void UpdateVolumes()
        {
            if (musicSource != null)
            {
                musicSource.volume = musicVolume * masterVolume;
            }

            if (ambientSource != null)
            {
                ambientSource.volume = ambientVolume * masterVolume;
            }

            AudioListener.volume = isMuted ? 0f : masterVolume;
        }

        // =====================
        // MUSIC METHODS
        // =====================

        public void PlayMusic(AudioClip clip, bool loop = true)
        {
            if (musicSource == null || clip == null) return;

            if (musicFadeCoroutine != null)
            {
                StopCoroutine(musicFadeCoroutine);
            }

            musicFadeCoroutine = StartCoroutine(CrossfadeMusic(clip, loop));
        }

        public void PlayMusic(string musicId)
        {
            AudioClip clip = musicId switch
            {
                "menu" => menuMusic,
                "gameplay" => gameplayMusic,
                "victory" => victoryMusic,
                "defeat" => defeatMusic,
                _ => null
            };

            if (clip != null)
            {
                PlayMusic(clip);
            }
        }

        public void StopMusic(bool fade = true)
        {
            if (musicSource == null) return;

            if (fade && musicFadeDuration > 0)
            {
                if (musicFadeCoroutine != null)
                {
                    StopCoroutine(musicFadeCoroutine);
                }
                musicFadeCoroutine = StartCoroutine(FadeMusicOut());
            }
            else
            {
                musicSource.Stop();
            }
        }

        public void PauseMusic()
        {
            if (musicSource != null)
            {
                musicSource.Pause();
            }
        }

        public void ResumeMusic()
        {
            if (musicSource != null)
            {
                musicSource.UnPause();
            }
        }

        private System.Collections.IEnumerator CrossfadeMusic(AudioClip newClip, bool loop)
        {
            float startVolume = musicSource.volume;

            // Fade out
            float elapsed = 0f;
            while (elapsed < musicFadeDuration / 2f)
            {
                elapsed += Time.unscaledDeltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / (musicFadeDuration / 2f));
                yield return null;
            }

            // Switch clip
            musicSource.Stop();
            musicSource.clip = newClip;
            musicSource.loop = loop;
            musicSource.Play();

            // Fade in
            elapsed = 0f;
            float targetVolume = musicVolume * masterVolume;
            while (elapsed < musicFadeDuration / 2f)
            {
                elapsed += Time.unscaledDeltaTime;
                musicSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / (musicFadeDuration / 2f));
                yield return null;
            }

            musicSource.volume = targetVolume;
            musicFadeCoroutine = null;
        }

        private System.Collections.IEnumerator FadeMusicOut()
        {
            float startVolume = musicSource.volume;
            float elapsed = 0f;

            while (elapsed < musicFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / musicFadeDuration);
                yield return null;
            }

            musicSource.Stop();
            musicFadeCoroutine = null;
        }

        // =====================
        // SFX METHODS
        // =====================

        public void PlaySfx(string sfxId)
        {
            if (sfxDictionary.TryGetValue(sfxId, out AudioClip clip))
            {
                PlaySfx(clip);
            }
            else
            {
                Log.Warning($"AudioManager: SFX '{sfxId}' not found.");
            }
        }

        public void PlaySfx(AudioClip clip, float volumeMultiplier = 1f, float pitchVariation = 0f)
        {
            if (clip == null) return;

            AudioSource source = GetNextSfxSource();
            source.clip = clip;
            source.volume = sfxVolume * masterVolume * volumeMultiplier;
            source.pitch = 1f + UnityEngine.Random.Range(-pitchVariation, pitchVariation);
            source.Play();
        }

        public void PlaySfxAtPosition(string sfxId, Vector3 position)
        {
            if (sfxDictionary.TryGetValue(sfxId, out AudioClip clip))
            {
                PlaySfxAtPosition(clip, position);
            }
        }

        public void PlaySfxAtPosition(AudioClip clip, Vector3 position, float volumeMultiplier = 1f)
        {
            if (clip == null) return;

            AudioSource.PlayClipAtPoint(clip, position, sfxVolume * masterVolume * volumeMultiplier);
        }

        public void PlaySfxOneShot(AudioClip clip, float volumeMultiplier = 1f)
        {
            if (clip == null || sfxPool.Length == 0) return;

            sfxPool[0].PlayOneShot(clip, sfxVolume * masterVolume * volumeMultiplier);
        }

        private AudioSource GetNextSfxSource()
        {
            sfxPoolIndex = (sfxPoolIndex + 1) % sfxPool.Length;
            return sfxPool[sfxPoolIndex];
        }

        // =====================
        // AMBIENT METHODS
        // =====================

        public void PlayAmbient(AudioClip clip, bool loop = true)
        {
            if (ambientSource == null || clip == null) return;

            ambientSource.clip = clip;
            ambientSource.loop = loop;
            ambientSource.volume = ambientVolume * masterVolume;
            ambientSource.Play();
        }

        public void StopAmbient()
        {
            if (ambientSource != null)
            {
                ambientSource.Stop();
            }
        }

        // =====================
        // VOLUME CONTROL
        // =====================

        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            UpdateVolumes();
        }

        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            UpdateVolumes();
        }

        public void SetSfxVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
        }

        public void SetAmbientVolume(float volume)
        {
            ambientVolume = Mathf.Clamp01(volume);
            UpdateVolumes();
        }

        public void ToggleMute()
        {
            isMuted = !isMuted;
            UpdateVolumes();
        }

        public void SetMute(bool mute)
        {
            isMuted = mute;
            UpdateVolumes();
        }

        // =====================
        // EVENT HANDLERS
        // =====================

        private void OnGamePaused(GamePausedEvent evt)
        {
            if (evt.IsPaused)
            {
                PauseMusic();
            }
            else
            {
                ResumeMusic();
            }
        }

        // =====================
        // GAME STATE SOUNDS
        // =====================

        public void PlayButtonClick()
        {
            PlaySfx("button_click");
        }

        public void PlayUnitSpawn()
        {
            PlaySfx("unit_spawn");
        }

        public void PlayTowerShoot()
        {
            PlaySfx("tower_shoot");
        }

        public void PlayHit()
        {
            PlaySfx("hit");
        }

        public void PlayDeath()
        {
            PlaySfx("death");
        }

        public void PlayVictory()
        {
            if (victoryMusic != null)
            {
                PlayMusic(victoryMusic, false);
            }
            PlaySfx("victory");
        }

        public void PlayDefeat()
        {
            if (defeatMusic != null)
            {
                PlayMusic(defeatMusic, false);
            }
            PlaySfx("defeat");
        }

        public void PlayWaveStart()
        {
            PlaySfx("wave_start");
        }

        public void PlayCardPlay()
        {
            PlaySfx("card_play");
        }

        public bool IsMuted => isMuted;
    }
}
