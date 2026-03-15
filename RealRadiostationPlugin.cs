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
            
            // 1. Сканируем карту на наличие существующих радиостанций (чтобы конфиг работал сразу)
            ScanMapForRadios();

            // 2. Подписываемся на события
            BarricadeManager.onBarricadeSpawned += OnBarricadeSpawned;
            PlayerVoice.onRelayVoice += OnVoiceRelay;
        }

        private void ScanMapForRadios()
        {
            ActiveStations.Clear();
            foreach (var region in BarricadeManager.regions)
            {
                foreach (var drop in region.drops)
                {
                    if (drop.asset.id == Configuration.Instance.RadioBarricadeId)
                    {
                        AddRadioComponent(drop);
                    }
                }
            }
        }

        private void OnVoiceRelay(PlayerVoice speaker, bool wantsToUseRadio, ref bool shouldAllow, ref bool shouldBroadcastOverRadio)
        {
            if (speaker == null || speaker.player == null) return;

            // Если игрок говорит просто голосом (Alt), проверяем, стоит ли он у радиостанции передачи
            if (!wantsToUseRadio)
            {
                var station = GetNearestStation(speaker.player.transform.position, 3f);
                if (station != null && station.Mode == RadioMode.TransmitAndListen)
                {
                    // ПРОФЕССИОНАЛЬНЫЙ ХАК: временно синхронизируем частоту игрока с частотой станции
                    speaker.player.quests.sendSetRadioFrequency(station.Frequency);
                    shouldBroadcastOverRadio = true; 
                    shouldAllow = true;
                }
            }
        }

        private void OnBarricadeSpawned(BarricadeRegion region, BarricadeDrop drop)
        {
            if (drop.asset.id == Configuration.Instance.RadioBarricadeId)
            {
                AddRadioComponent(drop);
            }
        }

        private void AddRadioComponent(BarricadeDrop drop)
        {
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

            foreach (var station in ActiveStations)
            {
                if (station == null) continue;
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
