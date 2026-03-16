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
            ScanExistingStations();
            BarricadeManager.onBarricadeSpawned += OnBarricadeSpawned;
            
            // Подписка на голос (тестируем 5-параметровую версию)
            PlayerVoice.onRelayVoice += OnVoiceRelay;
            
            Rocket.Core.Logging.Logger.Log("RealRadiostation загружен. Найдено станций: " + ActiveStations.Count);
        }

        private void ScanExistingStations()
        {
            ActiveStations.Clear();
            foreach (var region in BarricadeManager.regions)
            {
                foreach (var drop in region.drops)
                {
                    if (drop.asset.id == Configuration.Instance.RadioBarricadeId)
                    {
                        AddStationComponent(drop);
                    }
                }
            }
        }

        // РЕШЕНИЕ CS0123: Тестируем самую полную сигнатуру из новых версий Unturned
        private void OnVoiceRelay(PlayerVoice speaker, bool wantsToUseRadio, ref bool shouldAllow, ref bool shouldBroadcastOverRadio, ref float spatialBlend)
        {
            if (speaker?.player == null) return;

            if (!wantsToUseRadio)
            {
                var station = GetNearestStation(speaker.player.transform.position, Configuration.Instance.TransmitRadius);
                if (station != null && station.Mode == RadioMode.TransmitAndListen)
                {
                    // РЕШЕНИЕ CS1503: Явное приведение типа
                    uint freqToSet = (uint)station.Frequency;
                    speaker.player.quests.sendSetRadioFrequency(freqToSet);
                    
                    shouldAllow = true;
                    shouldBroadcastOverRadio = true; 
                }
            }
        }

        private void OnBarricadeSpawned(BarricadeRegion region, BarricadeDrop drop)
        {
            if (drop.asset.id == Configuration.Instance.RadioBarricadeId)
            {
                AddStationComponent(drop);
            }
        }

        private void AddStationComponent(BarricadeDrop drop)
        {
            if (drop.model.gameObject.GetComponent<RadioStationComponent>() == null)
            {
                var comp = drop.model.gameObject.AddComponent<RadioStationComponent>();
                ActiveStations.Add(comp);
            }
        }

        // РЕШЕНИЕ CS7036: Добавлено значение по умолчанию (maxDistance = 3f)
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
            PlayerVoice.onRelayVoice -= OnVoiceRelay;
            ActiveStations.Clear();
        }
    }
}
