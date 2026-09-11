using UnityEngine;

namespace AquaPath
{
    /// <summary>Lightweight synthesized audio and device-feedback controller.</summary>
    public sealed class AudioController : MonoBehaviour
    {
        public static AudioController Instance { get; private set; }

        public bool MusicEnabled { get { return SaveStore.MusicEnabled; } }
        public bool SfxEnabled { get { return SaveStore.SfxEnabled; } }
        public bool HapticsEnabled { get { return SaveStore.HapticsEnabled; } }

        private AudioSource musicSource;
        private AudioSource sfxSource;
        private AudioClip clickClip;
        private AudioClip turnClip;
        private AudioClip victoryClip;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            musicSource = gameObject.AddComponent<AudioSource>();
            sfxSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.volume = .12f;
            sfxSource.volume = .34f;

            clickClip = MakeTone("Soft Click", 620f, .055f, .13f);
            turnClip = MakeTone("Glass Turn", 410f, .09f, .18f);
            victoryClip = MakeChord();
            musicSource.clip = MakeAmbience();
            ApplyMusic();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void SetMusic(bool enabled)
        {
            SaveStore.MusicEnabled = enabled;
            ApplyMusic();
        }

        public void SetSfx(bool enabled) { SaveStore.SfxEnabled = enabled; }
        public void SetHaptics(bool enabled) { SaveStore.HapticsEnabled = enabled; }

        public void PlayClick() { Play(clickClip); }
        public void PlayTurn() { Play(turnClip); }

        public void PlayVictory()
        {
            Play(victoryClip);
            Haptic();
        }

        public void Haptic()
        {
            if (HapticsEnabled && Application.isMobilePlatform)
                Handheld.Vibrate();
        }

        private void ApplyMusic()
        {
            if (musicSource == null) return;
            if (MusicEnabled)
            {
                if (!musicSource.isPlaying) musicSource.Play();
            }
            else musicSource.Stop();
        }

        private void Play(AudioClip clip)
        {
            if (SfxEnabled && clip != null && sfxSource != null)
                sfxSource.PlayOneShot(clip);
        }

        private static AudioClip MakeTone(string name, float frequency, float duration, float volume)
        {
            const int sampleRate = 22050;
            int count = Mathf.CeilToInt(duration * sampleRate);
            float[] samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)sampleRate;
                float envelope = 1f - i / (float)count;
                samples[i] = Mathf.Sin(t * frequency * Mathf.PI * 2f) * envelope * volume;
            }
            AudioClip clip = AudioClip.Create(name, count, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip MakeChord()
        {
            const int sampleRate = 22050;
            const float duration = .7f;
            int count = Mathf.CeilToInt(duration * sampleRate);
            float[] samples = new float[count];
            float[] notes = { 523.25f, 659.25f, 783.99f };
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)sampleRate;
                float envelope = Mathf.Sin(Mathf.Clamp01(t / .08f) * Mathf.PI * .5f) * (1f - i / (float)count);
                float value = 0f;
                for (int n = 0; n < notes.Length; n++) value += Mathf.Sin(t * notes[n] * Mathf.PI * 2f);
                samples[i] = value / notes.Length * envelope * .18f;
            }
            AudioClip clip = AudioClip.Create("Water Complete", count, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip MakeAmbience()
        {
            const int sampleRate = 22050;
            const float duration = 8f;
            int count = Mathf.CeilToInt(duration * sampleRate);
            float[] samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)sampleRate;
                float fade = Mathf.Sin(Mathf.PI * i / count);
                float pad = Mathf.Sin(t * 110f * Mathf.PI * 2f) * .025f;
                pad += Mathf.Sin(t * 164.81f * Mathf.PI * 2f) * .016f;
                pad += Mathf.Sin(t * 220f * Mathf.PI * 2f) * .008f;
                samples[i] = pad * (.65f + .35f * fade);
            }
            AudioClip clip = AudioClip.Create("Aqua Ambience", count, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
