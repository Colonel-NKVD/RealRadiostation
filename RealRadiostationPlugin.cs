using Rocket.Core.Plugins;
using SDG.Unturned;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;

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

            // Harmony патчи
            harmony = new Harmony("com.realradiostation.patch");
            harmony.PatchAll();

            // ЗАПУСК ПОИСКА С ЗАДЕРЖКОЙ (решает проблему пустых регионов при старте)
            StartCoroutine(DeferredStationSearch());

            BarricadeManager.onBarricadeSpawned += OnBarricadeSpawned;
            
            Rocket.Core.Logging.Logger.Log("RealRadiostation загружен. Ожидание прогрузки карты...");
        }

        private IEnumerator DeferredStationSearch()
        {
            // Ждем 3 секунды, пока Unturned выставит все баррикады на карту
            yield return new WaitForSeconds(3f);
            ScanExistingStations();
            Rocket.Core.Logging.Logger.Log($"Поиск завершен. Найдено станций: {ActiveStations.Count}");
        }

        private void ScanExistingStations()
        {
            ActiveStations.Clear();
            
            // Ищем вообще все объекты на карте, у которых ID совпадает с нашим радио
            // Это медленнее, но зато находит ВСЁ
            var allBarricades = UnityEngine.Object.FindObjectsOfType<Transform>();
            foreach (var t in allBarricades)
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
                
                // Загружаем сохраненные данные, если они есть
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
                if (station == null || station.gameObject == null) 
                { 
                    ActiveStations.RemoveAt(i); 
                    continue; 
                }

                float sqrDist = (position - station.transform.position).sqrMagnitude;
                if (sqrDist < minSqrDist)
                {
                    minSqrDist = sqrDist;
                    nearest = station;
                }
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
                if (drop != null)
                {
                    dataToSave[drop.instanceID] = new StationData { Frequency = station.Frequency, Mode = station.Mode };
                }
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
