using Rocket.Core.Plugins;
using SDG.Unturned;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using System.Reflection; // Добавлено для профессионального поиска методов

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

            // ПРОФЕССИОНАЛЬНЫЙ ПАТЧ: ищем метод динамически, чтобы избежать ошибок сигнатуры
            var original = typeof(PlayerVoice).GetMethod("receiveRelayVoice", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public) 
                        ?? typeof(PlayerVoice).GetMethod("handleRelayVoiceInternal", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            var prefix = typeof(VoicePatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public);

            if (original != null && prefix != null)
            {
                harmony.Patch(original, new HarmonyMethod(prefix));
                Rocket.Core.Logging.Logger.Log("Harmony: Метод передачи голоса успешно найден и пропатчен.");
            }
            else
            {
                Rocket.Core.Logging.Logger.LogError("Harmony FATAL ERROR: Не удалось найти метод для патча!");
            }

            StartCoroutine(DeferredStationSearch());
            BarricadeManager.onBarricadeSpawned += OnBarricadeSpawned;
            
            Rocket.Core.Logging.Logger.Log("RealRadiostation загружен успешно.");
        }

        private IEnumerator DeferredStationSearch()
        {
            yield return new WaitForSeconds(3f);
            ScanExistingStations();
            Rocket.Core.Logging.Logger.Log($"Поиск завершен. Найдено станций: {ActiveStations.Count}");
        }

        private void ScanExistingStations()
        {
            ActiveStations.Clear();
            var allTransforms = UnityEngine.Object.FindObjectsOfType<Transform>();
            foreach (var t in allTransforms)
            {
                var drop = BarricadeManager.FindBarricadeByRootTransform(t);
                if (drop != null && drop.asset.id == Configuration.Instance.RadioBarricadeId)
                {
                    AddStationComponent(drop);
                }
            }
        }

        private void OnBarricadeSpawned(BarricadeRegion region, BarricadeDrop drop)
        {
            if (drop?.asset != null && drop.asset.id == Configuration.Instance.RadioBarricadeId)
            {
                AddStationComponent(drop);
            }
        }

        private void AddStationComponent(BarricadeDrop drop)
        {
            if (drop?.model == null) return;
            var existingComp = drop.model.gameObject.GetComponent<RadioStationComponent>();
            if (existingComp == null)
            {
                var comp = drop.model.gameObject.AddComponent<RadioStationComponent>();
                var savedData = DataStorage.Load();
                if (savedData != null && savedData.ContainsKey(drop.instanceID))
                {
                    comp.Frequency = savedData[drop.instanceID].Frequency;
                    comp.Mode = savedData[drop.instanceID].Mode;
                }
                ActiveStations.Add(comp);
            }
        }

        public RadioStationComponent GetNearestStation(Vector3 position, float maxDistance = 3f)
        {
            RadioStationComponent nearest = null;
            float minSqrDist = maxDistance * maxDistance;
            for (int i = ActiveStations.Count - 1; i >= 0; i--)
            {
                var station = ActiveStations[i];
                if (station == null || station.gameObject == null) { ActiveStations.RemoveAt(i); continue; }
                float sqrDist = (position - station.transform.position).sqrMagnitude;
                if (sqrDist < minSqrDist) { minSqrDist = sqrDist; nearest = station; }
            }
            return nearest;
        }

        public void SaveStations()
        {
            var dataToSave = new Dictionary<ulong, StationData>();
            foreach (var station in ActiveStations)
            {
                if (station == null) continue;
                var drop = BarricadeManager.FindBarricadeByRootTransform(station.transform);
                if (drop != null) dataToSave[drop.instanceID] = new StationData { Frequency = station.Frequency, Mode = station.Mode };
            }
            DataStorage.Save(dataToSave);
        }

        protected override void Unload()
        {
            StopAllCoroutines();
            BarricadeManager.onBarricadeSpawned -= OnBarricadeSpawned;
            if (harmony != null) harmony.UnpatchAll("com.realradiostation.patch");
            ActiveStations.Clear();
            Instance = null;
        }
    }
}
