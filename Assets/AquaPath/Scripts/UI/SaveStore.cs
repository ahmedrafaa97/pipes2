using UnityEngine;

namespace AquaPath
{
    /// <summary>Small, versioned PlayerPrefs facade for progression and player choices.</summary>
    public static class SaveStore
    {
        public const string Prefix = "AquaGlass.";
        public const int LevelCount = 30;

        private const string VersionKey = Prefix + "SaveVersion";
        private const string UnlockedKey = Prefix + "UnlockedLevel";
        private const string MusicKey = Prefix + "Music";
        private const string SfxKey = Prefix + "Sfx";
        private const string HapticsKey = Prefix + "Haptics";

        public static int UnlockedLevel
        {
            get { EnsureInitialized(); return Mathf.Clamp(PlayerPrefs.GetInt(UnlockedKey, 1), 1, LevelCount); }
        }

        public static bool MusicEnabled
        {
            get { EnsureInitialized(); return PlayerPrefs.GetInt(MusicKey, 1) != 0; }
            set { SetBool(MusicKey, value); }
        }

        public static bool SfxEnabled
        {
            get { EnsureInitialized(); return PlayerPrefs.GetInt(SfxKey, 1) != 0; }
            set { SetBool(SfxKey, value); }
        }

        public static bool HapticsEnabled
        {
            get { EnsureInitialized(); return PlayerPrefs.GetInt(HapticsKey, 1) != 0; }
            set { SetBool(HapticsKey, value); }
        }

        public static int BestStars(int level)
        {
            return Mathf.Clamp(PlayerPrefs.GetInt(LevelKey(level, "Stars"), 0), 0, 3);
        }

        public static int BestMoves(int level)
        {
            return Mathf.Max(0, PlayerPrefs.GetInt(LevelKey(level, "Moves"), 0));
        }

        public static void RecordVictory(int level, int stars, int moves)
        {
            level = Mathf.Clamp(level, 1, LevelCount);
            stars = Mathf.Clamp(stars, 0, 3);
            moves = Mathf.Max(0, moves);

            PlayerPrefs.SetInt(LevelKey(level, "Stars"), Mathf.Max(stars, BestStars(level)));
            int previousMoves = BestMoves(level);
            if (moves > 0 && (previousMoves == 0 || moves < previousMoves))
                PlayerPrefs.SetInt(LevelKey(level, "Moves"), moves);

            PlayerPrefs.SetInt(UnlockedKey, Mathf.Max(UnlockedLevel, Mathf.Min(LevelCount, level + 1)));
            PlayerPrefs.Save();
        }

        private static void EnsureInitialized()
        {
            if (PlayerPrefs.GetInt(VersionKey, 0) >= 1)
                return;

            PlayerPrefs.SetInt(VersionKey, 1);
            if (!PlayerPrefs.HasKey(UnlockedKey)) PlayerPrefs.SetInt(UnlockedKey, 1);
            if (!PlayerPrefs.HasKey(MusicKey)) PlayerPrefs.SetInt(MusicKey, 1);
            if (!PlayerPrefs.HasKey(SfxKey)) PlayerPrefs.SetInt(SfxKey, 1);
            if (!PlayerPrefs.HasKey(HapticsKey)) PlayerPrefs.SetInt(HapticsKey, 1);
            PlayerPrefs.Save();
        }

        private static void SetBool(string key, bool value)
        {
            EnsureInitialized();
            PlayerPrefs.SetInt(key, value ? 1 : 0);
            PlayerPrefs.Save();
        }

        private static string LevelKey(int level, string suffix)
        {
            return Prefix + "Level." + Mathf.Clamp(level, 1, LevelCount) + "." + suffix;
        }
    }
}
