using Rocket.Core.Plugins;
using SDG.Unturned;
using UnityEngine;
using System.Collections.Generic;
using HarmonyLib;
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
            Logger.Log("--- [RealRadiostation] ВЕРСИЯ: 4.0-HARMONY (DudeVoice Tech) ---");

            // Инициализация Harmony по методу DudeVoiceChat
            harmony = new Harmony("com.realradiostation.patch");
            harmony.PatchAll(); // Автоматически найдет VoicePatch.cs и применит его

            Level.onPostLevelLoaded += OnLevelLoaded;
            BarricadeManager.onBarricadeSpawned += OnBarricadeSpawned;

            if (Level.isLoaded) ScanStations();
            
            Logger.Log("[RealRadiostation] Плагин загружен и патчи применены.");
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
        }

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
            Level.onPostLevelLoaded -= OnLevelLoaded;
            BarricadeManager.onBarricadeSpawned -= OnBarricadeSpawned;
            
            harmony?.UnpatchAll("com.realradiostation.patch");
            ActiveStations.Clear();
            Instance = null;
        }
    }
}
