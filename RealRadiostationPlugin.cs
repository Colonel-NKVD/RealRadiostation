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

            // 1. Инициализация Harmony
            harmony = new Harmony("com.realradiostation.patch");
            try 
            {
                harmony.PatchAll();
            }
            catch (System.Exception ex)
            {
                Rocket.Core.Logging.Logger.LogError("Harmony Patch Error: " + ex.Message);
            }

            // 2. Сканирование станций только если карта уже загружена
            if (Level.isLoaded)
            {
                ScanExistingStations();
            }
            
            BarricadeManager.onBarricadeSpawned += OnBarricadeSpawned;
            
            Rocket.Core.Logging.Logger.Log("RealRadiostation успешно запущен!");
        }

        private void ScanExistingStations()
        {
            if (ActiveStations == null) ActiveStations = new List<RadioStationComponent>();
            ActiveStations.Clear();

            if (BarricadeManager.regions == null) return;

            foreach (var region in BarricadeManager.regions)
            {
                if (region?.drops == null) continue;
                foreach (var drop in region.drops)
                {
                    if (drop?.asset != null && drop.asset.id == Configuration.Instance.RadioBarricadeId)
                    {
                        AddStationComponent(drop);
                    }
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

            for (int i = ActiveStations.Count - 1; i >= 0; i--)
            {
                var station = ActiveStations[i];
                if (station == null) { ActiveStations.RemoveAt(i); continue; }

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
            try 
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
                // Вызов статического метода вашего DataStorage
                DataStorage.Save(dataToSave);
            }
            catch (System.Exception ex)
            {
                Rocket.Core.Logging.Logger.LogError("Save Error: " + ex.Message);
            }
        }

        protected override void Unload()
        {
            BarricadeManager.onBarricadeSpawned -= OnBarricadeSpawned;
            if (harmony != null) harmony.UnpatchAll("com.realradiostation.patch");
            ActiveStations.Clear();
            Instance = null;
        }
    }
}
