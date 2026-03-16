using Rocket.Core.Plugins;
using SDG.Unturned;
using UnityEngine;
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

            // 1. Проверяем, загружен ли конфиг
            if (Configuration == null || Configuration.Instance == null)
            {
                Rocket.Core.Logging.Logger.LogError("Ошибка: Конфигурация не загружена!");
                return;
            }

            // 2. Инициализируем Harmony максимально рано
            harmony = new Harmony("com.realradiostation.patch");
            try 
            {
                harmony.PatchAll();
            }
            catch (System.Exception ex)
            {
                Rocket.Core.Logging.Logger.LogError("Ошибка при применении патчей Harmony: " + ex.Message);
            }

            // 3. Чтобы избежать NRE при старте, сканируем станции только когда уровень готов
            if (Level.isLoaded)
            {
                ScanExistingStations();
            }
            
            BarricadeManager.onBarricadeSpawned += OnBarricadeSpawned;
            
            Rocket.Core.Logging.Logger.Log("RealRadiostation загружен успешно.");
        }

        private void ScanExistingStations()
        {
            // Защита: проверяем, инициализирован ли список и есть ли регионы на карте
            if (ActiveStations == null) ActiveStations = new List<RadioStationComponent>();
            ActiveStations.Clear();

            if (BarricadeManager.regions == null) return;

            foreach (var region in BarricadeManager.regions)
            {
                if (region?.drops == null) continue;

                foreach (var drop in region.drops)
                {
                    // Проверка на null для ассета и конфига
                    if (drop?.asset != null && Configuration?.Instance != null)
                    {
                        if (drop.asset.id == Configuration.Instance.RadioBarricadeId)
                        {
                            AddStationComponent(drop);
                        }
                    }
                }
            }
        }

        private void OnBarricadeSpawned(BarricadeRegion region, BarricadeDrop drop)
        {
            if (drop?.asset != null && Configuration?.Instance != null)
            {
                if (drop.asset.id == Configuration.Instance.RadioBarricadeId)
                {
                    AddStationComponent(drop);
                }
            }
        }

        private void AddStationComponent(BarricadeDrop drop)
        {
            if (drop?.model == null) return;

            if (drop.model.gameObject.GetComponent<RadioStationComponent>() == null)
            {
                var comp = drop.model.gameObject.AddComponent<RadioStationComponent>();
                ActiveStations.Add(comp);
            }
        }

        public RadioStationComponent GetNearestStation(Vector3 position, float maxDistance = 3f)
        {
            RadioStationComponent nearest = null;
            float minSqrDist = maxDistance * maxDistance;

            if (ActiveStations == null) return null;

            for (int i = ActiveStations.Count - 1; i >= 0; i--)
            {
                var station = ActiveStations[i];
                if (station == null) 
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
            if (DataStorage == null || ActiveStations == null) return;

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
            BarricadeManager.onBarricadeSpawned -= OnBarricadeSpawned;
            
            // Защита от NRE в Unload: проверяем существование объекта перед вызовом методов
            if (harmony != null)
            {
                harmony.UnpatchAll("com.realradiostation.patch");
            }

            if (ActiveStations != null)
            {
                ActiveStations.Clear();
            }

            Instance = null;
        }
    }
}
