using System;
using System.IO;
using System.Text.Json;

namespace DigitalMosquito
{
    public class PlayerSaveData
    {
        public int TotalKills { get; set; } = 0;
        public int TotalXP { get; set; } = 0;

        // Calculated from TotalKills and progression configuration (never independent source of truth)
        public int CurrentLevel => ProgressionManager.GetLevelForKills(TotalKills).Level;
        public string HighestUnlockedWeapon => ProgressionManager.GetLevelForKills(TotalKills).WeaponId;

        public string EquippedWeaponId { get; set; } = "hand";
        public bool SoundMuted { get; set; } = true;
        public bool JumpscaresEnabled { get; set; } = true;
        public double JumpscareChance { get; set; } = 0.25;
        public bool HudCollapsed { get; set; } = false;
        public double BaseSpeedMultiplier { get; set; } = 1.0;
        public string SelectedCreatureType { get; set; } = "Auto";
        public double ControlWidgetX { get; set; } = -1;
        public double ControlWidgetY { get; set; } = -1;
        public DateTime LastSavedUtc { get; set; } = DateTime.UtcNow;
    }

    public static class SaveManager
    {
        private static readonly string SaveDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DigitalMosquito"
        );

        private static readonly string SaveFilePath = Path.Combine(SaveDirectory, "savegame.json");
        private static readonly string FallbackFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "savegame.json");

        public static PlayerSaveData Load()
        {
            try
            {
                string targetPath = File.Exists(SaveFilePath) ? SaveFilePath : FallbackFilePath;
                if (File.Exists(targetPath))
                {
                    string json = File.ReadAllText(targetPath);
                    var data = JsonSerializer.Deserialize<PlayerSaveData>(json);
                    if (data != null)
                    {
                        return data;
                    }
                }
            }
            catch
            {
                // Fallback to fresh save on any serialization issue
            }

            return new PlayerSaveData();
        }

        public static void Save(PlayerSaveData data)
        {
            if (data == null) return;
            data.LastSavedUtc = DateTime.UtcNow;

            try
            {
                if (!Directory.Exists(SaveDirectory))
                {
                    Directory.CreateDirectory(SaveDirectory);
                }

                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SaveFilePath, json);
            }
            catch
            {
                try
                {
                    string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(FallbackFilePath, json);
                }
                catch
                {
                    // Ignore write failures to maintain uninterrupted gameplay
                }
            }
        }
    }
}
