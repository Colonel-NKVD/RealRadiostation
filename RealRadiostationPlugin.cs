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

        protected override void Load()
        {
            Instance = this;
            Logger.Log("-----------------------------------------------");
            Logger.Log("[RealRadiostation] ЗАПУСК ТОЧЕЧНОГО ПАТЧА...");

            harmony = new Harmony("com.realradiostation.patch");

            // 1. Массив точных имен методов, которые когда-либо отвечали за голос в Unturned
            string[] possibleMethodNames = new string[] 
            {
                "receiveRelayVoice",
                "handleRelayVoiceInternal",
                "askVoiceChat",
                "receiveVoice",
                "ServerReceiveVoice",
                "ReceiveVoiceChat"
            };

            MethodInfo targetMethod = null;

            // 2. Ищем строго по именам
            foreach (string methodName in possibleMethodNames)
            {
                targetMethod = typeof(PlayerVoice).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (targetMethod != null)
                {
                    break; // Нашли нужный метод, останавливаем поиск
                }
            }

            // 3. Результат поиска
            if (targetMethod != null)
            {
                var prefix = typeof(VoicePatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public);
                harmony.Patch(targetMethod, new HarmonyMethod(prefix));
                Logger.Log($"[DEBUG SUCCESS] БИНГО! Harmony привязан к методу голоса: {targetMethod.Name}");
            }
            else
            {
                Logger.LogError("[DEBUG FATAL] Точные имена методов не подошли. Плагин остановлен.");
                Logger.Log("--- СПИСОК ВОЗМОЖНЫХ МЕТОДОВ ДЛЯ АНАЛИЗА ---");
                
                // Выводим только те методы, в названии которых есть слова Voice или Relay
                var allMethods = typeof(PlayerVoice).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (var m in allMethods)
                {
                    string n = m.Name.ToLower();
                    if ((n.Contains("voice") || n.Contains("relay")) && !n.StartsWith("add_") && !n.StartsWith("remove_"))
                    {
                        Logger.Log($"КАНДИДАТ: {m.Name}");
                    }
                }
                Logger.Log("--------------------------------------------");
            }

            BarricadeManager.onBarricadeSpawned += OnBarricadeSpawned;
            Level.onPostLevelLoaded += OnLevelLoaded;
            if (Level.isLoaded) StartCoroutine(DeferredScan());
        }

        private IEnumerator DeferredScan()
        {
            yield return new WaitForSeconds(3f);
            OnLevelLoaded(0);
        }

        private void OnLevelLoaded(int level)
        {
            ActiveStations.Clear();
            if (BarricadeManager.regions == null) return;
            foreach (var region in BarricadeManager.regions)
                foreach (var drop in region.drops)
                    if (drop.asset.id == 1466) AddStationComponent(drop);
            Logger.Log($"[DEBUG] Активных станций 1466: {ActiveStations.Count}");
        }

        private void OnBarricadeSpawned(BarricadeRegion region, BarricadeDrop drop)
        {
            if (drop.asset.id == 1466) AddStationComponent(drop);
        }

        private void AddStationComponent(BarricadeDrop drop)
        {
            var comp = drop.model.gameObject.GetComponent<RadioStationComponent>() 
                       ?? drop.model.gameObject.AddComponent<RadioStationComponent>();
            
            string hash = GetPosHash(drop.model.position);
            var savedData = DataStorage.Load();
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

        public string GetPosHash(Vector3 pos) => $"{pos.x:F1}_{pos.y:F1}_{pos.z:F1}";

        public void SaveAllStations()
        {
            var dict = new Dictionary<string, StationData>();
            foreach (var s in ActiveStations)
                if (s != null) dict[GetPosHash(s.transform.position)] = new StationData { Frequency = s.Frequency, Mode = s.Mode };
            DataStorage.Save(dict);
        }

        public RadioStationComponent GetNearestStation(Vector3 position, float radius)
        {
            RadioStationComponent nearest = null;
            float minSqrDist = radius * radius;
            foreach (var station in ActiveStations)
            {
                if (station == null) continue;
                float sqrDist = (position - station.transform.position).sqrMagnitude;
                if (sqrDist < minSqrDist) { minSqrDist = sqrDist; nearest = station; }
            }
            return nearest;
        }

        protected override void Unload()
        {
            if (harmony != null) harmony.UnpatchAll("com.realradiostation.patch");
            BarricadeManager.onBarricadeSpawned -= OnBarricadeSpawned;
            Level.onPostLevelLoaded -= OnLevelLoaded;
            ActiveStations.Clear();
            Instance = null;
        }
    }
}
