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
            Logger.Log("[RealRadiostation] ИНИЦИАЛИЗАЦИЯ (ID: 1466)");

            harmony = new Harmony("com.realradiostation.patch");

            // Улучшенный поиск метода: исключаем системные методы событий
            var allMethods = typeof(PlayerVoice).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo targetMethod = null;

            foreach (var m in allMethods)
            {
                string n = m.Name;
                // Ищем метод передачи, который НЕ является подпиской (add_/remove_) и НЕ является делегатом
                if ((n.Contains("RelayVoice") || n.Contains("receiveRelayVoice")) 
                    && !n.StartsWith("add_") && !n.StartsWith("remove_") && !n.Contains("Handler"))
                {
                    targetMethod = m;
                    break;
                }
            }

            if (targetMethod != null)
            {
                var prefix = typeof(VoicePatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public);
                harmony.Patch(targetMethod, new HarmonyMethod(prefix));
                Logger.Log($"[DEBUG SUCCESS] Harmony привязан к ПРАВИЛЬНОМУ методу: {targetMethod.Name}");
            }
            else
            {
                Logger.LogError("[DEBUG FATAL] Метод ретрансляции голоса не найден!");
            }

            BarricadeManager.onBarricadeSpawned += OnBarricadeSpawned;
            Level.onPostLevelLoaded += OnLevelLoaded;
            if (Level.isLoaded) StartCoroutine(DeferredScan());

            Logger.Log("[RealRadiostation] Плагин готов.");
            Logger.Log("-----------------------------------------------");
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
            Logger.Log($"[DEBUG] Загружено станций из мира: {ActiveStations.Count}");
        }

        private void OnBarricadeSpawned(BarricadeRegion region, BarricadeDrop drop)
        {
            if (drop.asset.id == 1466)
            {
                Logger.Log($"[DEBUG] Новая станция 1466 обнаружена (Instance: {drop.instanceID})");
                AddStationComponent(drop);
            }
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
            Logger.Log($"[DEBUG] Станция активна: {hash} | Частота: {comp.Frequency}");
        }

        public string GetPosHash(Vector3 pos) => $"{pos.x:F1}_{pos.y:F1}_{pos.z:F1}";

        // Этот метод теперь называется SaveAllStations, как того требует CommandRadio.cs
        public void SaveAllStations()
        {
            var dict = new Dictionary<string, StationData>();
            foreach (var s in ActiveStations)
            {
                if (s != null) dict[GetPosHash(s.transform.position)] = new StationData { Frequency = s.Frequency, Mode = s.Mode };
            }
            DataStorage.Save(dict);
            Logger.Log($"[DEBUG] Успешное сохранение {dict.Count} станций в JSON.");
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
