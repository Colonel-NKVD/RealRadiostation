using Rocket.Core.Plugins;
using SDG.Unturned;
using UnityEngine;
using System;
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

        protected override void Load()
        {
            Instance = this;
            harmony = new Harmony("com.realradiostation.patch");

            // Список классов, которые могут участвовать в передаче голоса
            Type[] targetTypes = { typeof(PlayerVoice), typeof(PlayerQuests) };
            
            int totalPatches = 0;
            foreach (var type in targetTypes)
            {
                var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                foreach (var method in methods)
                {
                    // Игнорируем совсем мусорные методы
                    if (method.Name.Contains("Update") || method.Name.StartsWith("get_") || method.Name.Contains("ToString")) continue;

                    try {
                        var prefix = typeof(VoicePatch).GetMethod("UniversalPrefix", BindingFlags.Static | BindingFlags.Public);
                        harmony.Patch(method, new HarmonyMethod(prefix));
                        totalPatches++;
                    } catch { }
                }
            }

            Logger.Log($"[DEBUG] Глобальный перехват активен. Установлено {totalPatches} патчей.");
            ScanStations();
        }

        public void ScanStations() 
        {
            // (Твой существующий код сканирования остается без изменений)
            ActiveStations.Clear();
            if (BarricadeManager.regions == null) return;
            foreach (var region in BarricadeManager.regions)
                foreach (var drop in region.drops)
                    if (drop.asset.id == 1466)
                    {
                        var comp = drop.model.gameObject.GetComponent<RadioStationComponent>() ?? drop.model.gameObject.AddComponent<RadioStationComponent>();
                        comp.Frequency = 111111;
                        ActiveStations.Add(comp);
                    }
        }

        public RadioStationComponent GetNearestStation(Vector3 pos, float radius)
        {
            foreach (var s in ActiveStations)
                if (s != null && Vector3.Distance(s.transform.position, pos) <= radius) return s;
            return null;
        }

        public void SaveAllStations() { }
        public string GetPosHash(Vector3 pos) => $"{pos.x:F1}_{pos.y:F1}_{pos.z:F1}";

        protected override void Unload()
        {
            harmony?.UnpatchAll("com.realradiostation.patch");
        }
    }
}
