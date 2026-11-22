using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PuppetDungeon
{
    /// <summary>Musique (fondu enchaîné) + pool de sources pour les effets, avec variation de hauteur.</summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private AudioSource musicA, musicB;
        private bool aActive = true;
        private readonly List<AudioSource> pool = new List<AudioSource>();
        private int next;
        private AudioClip currentMusic;
        private float musicCueVolume = 1f;
        private readonly Dictionary<SoundCue, float> lastPlayed = new Dictionary<SoundCue, float>();
        private readonly Dictionary<SoundCue, float> lastPitch = new Dictionary<SoundCue, float>();
        private readonly Dictionary<AudioSource, SoundCue> playingCue = new Dictionary<AudioSource, SoundCue>();
        private readonly Dictionary<AudioSource, AudioLowPassFilter> filters = new Dictionary<AudioSource, AudioLowPassFilter>();

        public AudioLibrary Lib => GameManager.Instance != null && GameManager.Instance.Config != null ? GameManager.Instance.Config.audio : null;

        private void Awake()
        {
            Instance = this;
            musicA = CreateSource("Music A", true);
            musicB = CreateSource("Music B", true);
            for (int i = 0; i < 16; i++)
            {
                var s = CreateSource("Sfx " + i, false);
                // Filtre passe-bas intégré d'Unity : adoucit les sons de bois très secs.
                filters[s] = s.gameObject.AddComponent<AudioLowPassFilter>();
                pool.Add(s);
            }
        }

        private AudioSource CreateSource(string n, bool loop)
        {
            var go = new GameObject(n);
            go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = loop;
            s.spatialBlend = 0f;
            return s;
        }

        public void RefreshVolumes()
        {
            float lib = Lib != null ? Lib.musicVolume : 0.6f;
            var active = aActive ? musicA : musicB;
            if (active != null) active.volume = lib * Settings.Music * musicCueVolume;
        }

        public void PlayMusic(AudioClip clip, float fade = 1.2f, float volume = 1f)
        {
            if (clip == null || clip == currentMusic) return;
            currentMusic = clip;
            musicCueVolume = volume;
            StopAllCoroutines();
            StartCoroutine(Crossfade(clip, fade));
        }

        private IEnumerator Crossfade(AudioClip clip, float fade)
        {
            var from = aActive ? musicA : musicB;
            var to = aActive ? musicB : musicA;
            aActive = !aActive;
            float target = (Lib != null ? Lib.musicVolume : 0.6f) * Settings.Music * musicCueVolume;
            to.clip = clip;
            to.volume = 0f;
            to.Play();
            float startFrom = from.volume;
            for (float t = 0; t < fade; t += Time.unscaledDeltaTime)
            {
                float k = t / fade;
                to.volume = Mathf.Lerp(0f, target, k);
                from.volume = Mathf.Lerp(startFrom, 0f, k);
                yield return null;
            }
            to.volume = target;
            from.Stop();
        }

        /// <summary>Joue un son de la bibliothèque. pitchMul permet de réutiliser un son (ex : porte = coup grave).</summary>
        public void Play(SoundCue cue, float volumeMul = 1f, float pitchMul = 1f, float minInterval = 0.06f)
        {
            if (cue == null || !cue.IsValid) return;
            float now = Time.unscaledTime;
            if (lastPlayed.TryGetValue(cue, out float last) && now - last < minInterval) return;
            lastPlayed[cue] = now;
            var clip = cue.clips[Random.Range(0, cue.clips.Length)];
            if (clip == null) return;
            var lib = Lib;
            float jitter = lib != null ? lib.pitchJitter : 0.1f;

            // Pas plus de N exemplaires du même son en même temps (les coups en rafale ne s'empilent plus).
            int maxVoices = lib != null ? Mathf.Max(1, lib.maxVoicesPerSound) : 2;
            int voices = 0;
            foreach (var kv in playingCue) if (kv.Value == cue && kv.Key.isPlaying) voices++;
            if (voices >= maxVoices) return;

            // Hauteur : plage du son × variation aléatoire, jamais deux fois de suite (presque) la même.
            float pitch = Random.Range(cue.pitch.x, cue.pitch.y) * (1f + Random.Range(-jitter, jitter));
            if (lastPitch.TryGetValue(cue, out float lp) && Mathf.Abs(pitch - lp) < jitter * 0.35f)
                pitch *= pitch >= lp ? 1f + jitter * 0.5f : 1f - jitter * 0.5f;
            lastPitch[cue] = pitch;

            var s = pool[next];
            next = (next + 1) % pool.Count;
            s.clip = clip;
            s.pitch = pitch * pitchMul;
            float volJitter = lib != null ? lib.volumeJitter : 0.2f;
            float master = lib != null ? lib.sfxVolume : 0.4f;
            // Un second exemplaire simultané est plus discret.
            float stack = voices > 0 ? 0.6f : 1f;
            s.volume = cue.volume * volumeMul * Settings.Sfx * master * stack * (1f - Random.Range(0f, volJitter));
            if (filters.TryGetValue(s, out var lpf) && lpf != null)
            {
                float cutoff = lib != null ? lib.lowPassCutoff : 5500f;
                lpf.enabled = cutoff < 21000f;
                lpf.cutoffFrequency = cutoff;
            }
            playingCue[s] = cue;
            s.Play();
        }

        public static void Sfx(System.Func<AudioLibrary, SoundCue> pick, float volumeMul = 1f, float pitchMul = 1f)
        {
            if (Instance == null || Instance.Lib == null) return;
            Instance.Play(pick(Instance.Lib), volumeMul, pitchMul);
        }
    }
}
