using Rocket.Core.Plugins;
using SDG.Unturned;
using UnityEngine; // Обязательно для Vector3
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

        protected override void Load()
        {
            Instance = this;
            Logger.Log("-----------------------------------------------");
            Logger.Log("[RealRadiostation] ЗАПУСК ДЕТЕКТОР-ПАТЧА...");

            harmony = new Harmony("com.realradiostation.patch");

            // Список всех возможных методов-претендентов для разных версий Unturned
            string[] voiceMethods = { "askVoiceChat", "receiveVoiceChat", "handleVoiceChat", "ReceiveVoice" };
            
            int patchCount = 0;
            foreach (var name in voiceMethods)
            {
                var method = typeof(PlayerVoice).GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (method != null)
                {
                    var prefix = typeof(VoicePatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public);
                    harmony.Patch(method, new HarmonyMethod(prefix));
                    Logger.Log($"[DEBUG] Патч установлен на: {name}");
                    patchCount++;
                }
            }

            if (patchCount == 0) Logger.LogError("[FATAL] Ни один метод голоса не найден!");

            Level.onPostLevelLoaded += OnLevelLoaded;
            BarricadeManager.onBarricadeSpawned += OnBarricadeSpawned;
            
            if (Level.isLoaded) ScanStations();

            Logger.Log("[RealRadiostation] Режим детектива активен.");
            Logger.Log("-----------------------------------------------");
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
            foreach (var region in BarricadeManager.regions)
                foreach (var drop in region.drops)
                    if (drop.asset.id == 1466)
                    {
                        var comp = drop.model.gameObject.GetComponent<RadioStationComponent>() 
                                   ?? drop.model.gameObject.AddComponent<RadioStationComponent>();
                        
                        comp.Frequency = 111111; 
                        comp.Mode = RadioMode.TransmitAndListen;
                        ActiveStations.Add(comp);
                    }
            Logger.Log($"[DEBUG] Станций в списке: {ActiveStations.Count}");
        }

        public RadioStationComponent GetNearestStation(Vector3 pos, float radius)
        {
            foreach (var s in ActiveStations)
            {
                if (s == null) continue;
                if (Vector3.Distance(s.transform.position, pos) <= radius) return s;
            }
            return null;
        }

        protected override void Unload()
        {
            if (harmony != null) harmony.UnpatchAll("com.realradiostation.patch");
            ActiveStations.Clear();
            Instance = null;
        }
    }
}
