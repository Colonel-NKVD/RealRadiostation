using Rocket.Core.Plugins;
using SDG.Unturned;
using UnityEngine;
using System.Collections.Generic;
using HarmonyLib;
using System.Reflection;
using Logger = Rocket.Core.Logging.Logger;

namespace RealRadiostation
{
    public class RealRadiostationPlugin : RocketPlugin<RealRadiostationConfig>
    {
        public static RealRadiostationPlugin Instance;
        public List<RadioStationComponent> ActiveStations = new List<RadioStationComponent>();
        private Harmony harmony;
        public string PluginVersion = "1.0.5-DEBUG"; // Меняйте это при каждом билде!

        protected override void Load()
        {
            Instance = this;
            Logger.Log($"--- [RealRadiostation] ВЕРСИЯ: {PluginVersion} ---");

            harmony = new Harmony("com.realradiostation.patch");

            // ПАТЧИМ ВСЁ: Находим вообще все методы в классе PlayerVoice
            var allMethods = typeof(PlayerVoice).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            int count = 0;

            foreach (var method in allMethods)
            {
                // Игнорируем стандартные системные методы
                if (method.Name.Contains("Get") || method.Name.Contains("Set") || method.Name.StartsWith("add_") || method.Name.StartsWith("remove_")) 
                    continue;

                try 
                {
                    var prefix = typeof(VoicePatch).GetMethod("UniversalPrefix", BindingFlags.Static | BindingFlags.Public);
                    harmony.Patch(method, new HarmonyMethod(prefix));
                    count++;
                }
                catch { /* Пропускаем методы, которые нельзя патчить */ }
            }

            Logger.Log($"[DEBUG] Успешно установлено {count} патчей на методы PlayerVoice.");

            Level.onPostLevelLoaded += (int level) => ScanStations();
            BarricadeManager.onBarricadeSpawned += (region, drop) => { if (drop.asset.id == 1466) ScanStations(); };
            if (Level.isLoaded) ScanStations();
        }

        public void ScanStations()
        {
            ActiveStations.Clear();
            if (BarricadeManager.regions == null) return;
            foreach (var region in BarricadeManager.regions)
                foreach (var drop in region.drops)
                    if (drop.asset.id == 1466)
                    {
                        var comp = drop.model.gameObject.GetComponent<RadioStationComponent>() ?? drop.model.gameObject.AddComponent<RadioStationComponent>();
                        comp.Frequency = 111111; 
                        comp.Mode = RadioMode.TransmitAndListen;
                        ActiveStations.Add(comp);
                    }
        }

        public void SaveAllStations() { /* Метод для команд */ }

        protected override void Unload()
        {
            harmony?.UnpatchAll("com.realradiostation.patch");
            ActiveStations.Clear();
            Instance = null;
        }
    }
}
