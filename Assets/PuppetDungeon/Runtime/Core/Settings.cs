using UnityEngine;

namespace PuppetDungeon
{
    /// <summary>Options joueur sauvegardées dans PlayerPrefs.</summary>
    public static class Settings
    {
        private const string KMaster = "pd_master", KMusic = "pd_music", KSfx = "pd_sfx", KShake = "pd_shake", KHitStop = "pd_hitstop", KDof = "pd_dof";

        public static float Master
        {
            get => PlayerPrefs.GetFloat(KMaster, 0.9f);
            set { PlayerPrefs.SetFloat(KMaster, value); Apply(); }
        }

        public static float Music
        {
            get => PlayerPrefs.GetFloat(KMusic, 0.7f);
            set { PlayerPrefs.SetFloat(KMusic, value); Apply(); }
        }

        public static float Sfx
        {
            get => PlayerPrefs.GetFloat(KSfx, 0.85f);
            set { PlayerPrefs.SetFloat(KSfx, value); Apply(); }
        }

        public static float ScreenShake
        {
            get => PlayerPrefs.GetFloat(KShake, 1f);
            set => PlayerPrefs.SetFloat(KShake, value);
        }

        public static bool HitStop
        {
            get => PlayerPrefs.GetInt(KHitStop, 1) == 1;
            set => PlayerPrefs.SetInt(KHitStop, value ? 1 : 0);
        }

        public static bool TiltShift
        {
            get => PlayerPrefs.GetInt(KDof, 1) == 1;
            set => PlayerPrefs.SetInt(KDof, value ? 1 : 0);
        }

        public static void Apply()
        {
            AudioListener.volume = Master;
            if (AudioManager.Instance != null) AudioManager.Instance.RefreshVolumes();
        }
    }
}
