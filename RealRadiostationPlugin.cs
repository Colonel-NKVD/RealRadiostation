using Rocket.Core.Plugins;
using SDG.Unturned;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using System.Reflection;

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

            // Инициализация Harmony
            harmony = new Harmony("com.realradiostation.patch");

            // Ищем метод динамически. В разных версиях он может называться по-разному.
            var original = typeof(PlayerVoice).GetMethod("receiveRelayVoice", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public) 
                        ?? typeof(PlayerVoice).GetMethod("handleRelayVoiceInternal", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            var prefix = typeof(VoicePatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public);

            if (original != null && prefix != null)
            {
                harmony.Patch(original, new HarmonyMethod(prefix));
                Rocket.Core.Logging.Logger.Log($"[DEBUG] Harmony успешно пропатчил: {original.Name}");
            }
            else
            {
                Rocket.Core.Logging.Logger.LogError("[DEBUG FATAL] Не удалось найти метод для патча голоса!");
            }

            StartCoroutine(DeferredStationSearch());
            BarricadeManager.onBarricadeSpawned += OnBarricadeSpawned;
            Level.onPostLevelLoaded += OnLevelLoaded;

            if (Level.isLoaded) OnLevelLoaded(0);
        }

        // ... (остальные методы ScanExistingStations, AddStationComponent и т.д. остаются как были)

        protected override void Unload()
        {
            if (harmony != null) harmony.UnpatchAll("com.realradiostation.patch");
            BarricadeManager.onBarricadeSpawned -= OnBarricadeSpawned;
            Level.onPostLevelLoaded -= OnLevelLoaded;
            ActiveStations.Clear();
            Instance = null;
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
            {
                if (s != null) dict[GetPosHash(s.transform.position)] = new StationData { Frequency = s.Frequency, Mode = s.Mode };
            }
            DataStorage.Save(dict);
        }

        private void OnLevelLoaded(int level)
        {
            ActiveStations.Clear();
            if (BarricadeManager.regions == null) return;
            foreach (var region in BarricadeManager.regions)
                foreach (var drop in region.drops)
                    if (drop.asset.id == Configuration.Instance.RadioBarricadeId) AddStationComponent(drop);
        }

        private void OnBarricadeSpawned(BarricadeRegion region, BarricadeDrop drop)
        {
            if (drop.asset.id == Configuration.Instance.RadioBarricadeId) AddStationComponent(drop);
        }

        private void AddStationComponent(BarricadeDrop drop)
        {
            var comp = drop.model.gameObject.GetComponent<RadioStationComponent>() ?? drop.model.gameObject.AddComponent<RadioStationComponent>();
            string hash = GetPosHash(drop.model.position);
            var savedData = DataStorage.Load();
            if (savedData != null && savedData.ContainsKey(hash)) { comp.Frequency = savedData[hash].Frequency; comp.Mode = savedData[hash].Mode; }
            else { comp.Frequency = 111111; comp.Mode = RadioMode.TransmitAndListen; }
            if (!ActiveStations.Contains(comp)) ActiveStations.Add(comp);
        }

        private IEnumerator DeferredStationSearch()
        {
            yield return new WaitForSeconds(3f);
            OnLevelLoaded(0);
        }
    }
}
