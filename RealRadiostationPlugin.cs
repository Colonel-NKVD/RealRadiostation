using Rocket.Core.Plugins;
using SDG.Unturned;
using UnityEngine;
using System.Collections;
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
        public string PluginVersion = "1.0.6-DEBUG";

        protected override void Load()
        {
            Instance = this;
            Logger.Log($"--- [RealRadiostation] ВЕРСИЯ: {PluginVersion} ---");

            harmony = new Harmony("com.realradiostation.patch");

            // Привязываемся ко ВСЕМ методам PlayerVoice для поиска работающего захвата
            var allMethods = typeof(PlayerVoice).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            int count = 0;
            foreach (var method in allMethods)
            {
                if (method.Name.Contains("Get") || method.Name.Contains("Set") || method.Name.StartsWith("add_") || method.Name.StartsWith("remove_")) 
                    continue;

                try 
                {
                    var prefix = typeof(VoicePatch).GetMethod("UniversalPrefix", BindingFlags.Static | BindingFlags.Public);
                    harmony.Patch(method, new HarmonyMethod(prefix));
                    count++;
                }
                catch { }
            }

            Logger.Log($"[DEBUG] Установлено {count} патчей на PlayerVoice.");

            Level.onPostLevelLoaded += OnLevelLoaded;
            BarricadeManager.onBarricadeSpawned += OnBarricadeSpawned;
            
            if (Level.isLoaded) ScanStations();
            Logger.Log("[RealRadiostation] Плагин готов к диагностике.");
        }

        private void OnLevelLoaded(int level) => ScanStations();
        private void OnBarricadeSpawned(BarricadeRegion region, BarricadeDrop drop) 
        { 
            if (drop.asset.id == 1466) ScanStations(); 
        }

        public void ScanStations()
        {
            ActiveStations.Clear();
            if (BarricadeManager.regions == null) return;
            var savedData = DataStorage.Load();

            foreach (var region in BarricadeManager.regions)
            {
                foreach (var drop in region.drops)
                {
                    if (drop.asset.id == 1466)
                    {
                        var comp = drop.model.gameObject.GetComponent<RadioStationComponent>() 
                                   ?? drop.model.gameObject.AddComponent<RadioStationComponent>();
                        
                        string hash = GetPosHash(drop.model.position);
                        if (savedData != null && savedData.ContainsKey(hash))
                        {
                            comp.Frequency = savedData[hash].Frequency;
                            comp.Mode = savedData[hash].Mode;
                        }
                        else
                        {
                            comp.Frequency = 111111; 
                            comp.Mode = RadioMode.TransmitAndListen;
                        }
                        if (!ActiveStations.Contains(comp)) ActiveStations.Add(comp);
                    }
                }
            }
            Logger.Log($"[DEBUG] Найдено станций: {ActiveStations.Count}");
        }

        // Возвращаем методы, которые требовал компилятор:
        public string GetPosHash(Vector3 pos) => $"{pos.x:F1}_{pos.y:F1}_{pos.z:F1}";

        public void SaveAllStations()
        {
            var dict = new Dictionary<string, StationData>();
            foreach (var s in ActiveStations)
            {
                if (s != null) dict[GetPosHash(s.transform.position)] = new StationData { Frequency = s.Frequency, Mode = s.Mode };
            }
            DataStorage.Save(dict);
        }

        public RadioStationComponent GetNearestStation(Vector3 pos, float radius)
        {
            RadioStationComponent nearest = null;
            float minSqrDist = radius * radius;
            foreach (var s in ActiveStations)
            {
                if (s == null) continue;
                float sqrDist = (s.transform.position - pos).sqrMagnitude;
                if (sqrDist < minSqrDist)
                {
                    minSqrDist = sqrDist;
                    nearest = s;
                }
            }
            return nearest;
        }

        protected override void Unload()
        {
            harmony?.UnpatchAll("com.realradiostation.patch");
            ActiveStations.Clear();
            Instance = null;
        }
    }
}
