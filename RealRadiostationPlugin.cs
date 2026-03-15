using Rocket.Core.Plugins;
using SDG.Unturned;
using UnityEngine;
using System.Collections.Generic;

namespace RealRadiostation
{
    public class RealRadiostationPlugin : RocketPlugin<RealRadiostationConfig>
    {
        public static RealRadiostationPlugin Instance;
        public List<RadioStationComponent> ActiveStations = new List<RadioStationComponent>();

        protected override void Load()
        {
            Instance = this;
            BarricadeManager.onBarricadeSpawned += OnBarricadeSpawned;
        }

        private void OnBarricadeSpawned(BarricadeRegion region, BarricadeDrop drop)
        {
            if (drop.asset.id == Configuration.Instance.RadioBarricadeId)
            {
                var comp = drop.model.gameObject.AddComponent<RadioStationComponent>();
                ActiveStations.Add(comp);
            }
        }

        // Метод сохранения данных всех активных станций
        public void SaveStations()
        {
            var dataToSave = new Dictionary<ulong, StationData>();
            foreach (var station in ActiveStations)
            {
                var drop = BarricadeManager.FindBarricadeByRootTransform(station.transform);
                if (drop != null)
                {
                    dataToSave[drop.instanceID] = new StationData 
                    { 
                        Frequency = station.Frequency, 
                        Mode = station.Mode 
                    };
                }
            }
            DataStorage.Save(dataToSave);
        }

        // Поиск ближайшей станции в радиусе 3 метров
        public RadioStationComponent GetNearestStation(Vector3 position)
        {
            RadioStationComponent nearest = null;
            float minDistance = 3f;
            foreach (var station in ActiveStations)
            {
                float dist = Vector3.Distance(position, station.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearest = station;
                }
            }
            return nearest;
        }

        protected override void Unload()
        {
            BarricadeManager.onBarricadeSpawned -= OnBarricadeSpawned;
            ActiveStations.Clear();
        }
    }
}
