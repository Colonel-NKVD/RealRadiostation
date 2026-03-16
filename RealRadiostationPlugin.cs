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
            Logger.Log("[RealRadiostation] ЗАПУСК ПЛАГИНА (ID: 1466)");

            harmony = new Harmony("com.realradiostation.patch");

            // ГЛУБОКИЙ СКАНЕР: Если метод не найден, мы выведем всё, что есть в классе
            var allMethods = typeof(PlayerVoice).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            
            MethodInfo targetMethod = null;
            foreach (var m in allMethods)
            {
                // Ищем наиболее вероятных кандидатов по названию
                if (m.Name.Contains("receiveRelayVoice") || m.Name.Contains("handleRelayVoiceInternal") || m.Name.Contains("RelayVoice"))
                {
                    targetMethod = m;
                    break;
                }
            }

            if (targetMethod != null)
            {
                var prefix = typeof(VoicePatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public);
                harmony.Patch(targetMethod, new HarmonyMethod(prefix));
                Logger.Log($"[DEBUG SUCCESS] Harmony привязан к методу: {targetMethod.Name}");
            }
            else
            {
                Logger.LogError("[DEBUG FATAL] Нужный метод не найден! Список доступных методов в PlayerVoice:");
                foreach (var m in allMethods) Logger.Log($" -> {m.Name}");
            }

            BarricadeManager.onBarricadeSpawned += OnBarricadeSpawned;
            Level.onPostLevelLoaded += OnLevelLoaded;
            if (Level.isLoaded) StartCoroutine(DeferredScan());

            Logger.Log("[RealRadiostation] Плагин готов к работе.");
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
                    if (drop.asset.id == 1466 || drop.asset.id == Configuration.Instance.RadioBarricadeId) AddStationComponent(drop);
            
            Logger.Log($"[DEBUG] Мир просканирован. Активных станций: {ActiveStations.Count}");
        }

        private void OnBarricadeSpawned(BarricadeRegion region, BarricadeDrop drop)
        {
            if (drop.asset.id == 1466 || drop.asset.id == Configuration.Instance.RadioBarricadeId)
            {
                Logger.Log($"[DEBUG] Установлена станция {drop.asset.id}. Мгновенная активация.");
                AddStationComponent(drop);
            }
        }

        private void AddStationComponent(BarricadeDrop drop)
        {
            var comp = drop.model.gameObject.GetComponent<RadioStationComponent>() ?? drop.model.gameObject.AddComponent<RadioStationComponent>();
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
            Logger.Log($"[DEBUG] Станция в реестре: {hash} | Частота: {comp.Frequency}");
        }

        public string GetPosHash(Vector3 pos) => $"{pos.x:F1}_{pos.y:F1}_{pos.z:F1}";

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

        public void SaveAllStations()
        {
            var dict = new Dictionary<string, StationData>();
            foreach (var s in ActiveStations)
                if (s != null) dict[GetPosHash(s.transform.position)] = new StationData { Frequency = s.Frequency, Mode = s.Mode };
            DataStorage.Save(dict);
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
