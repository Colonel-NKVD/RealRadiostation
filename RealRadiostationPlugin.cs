using Rocket.Core.Plugins;
using SDG.Unturned;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Logger = Rocket.Core.Logging.Logger;

namespace RealRadiostation
{
    public class RealRadiostationPlugin : RocketPlugin<RealRadiostationConfig>
    {
        public static RealRadiostationPlugin Instance;
        public List<RadioStationComponent> ActiveStations = new List<RadioStationComponent>();

        protected override void Load()
        {
            Instance = this;

            // Используем нативный API Unturned для 100% стабильности голоса
            PlayerVoice.onRelayVoice += OnRelayVoice;
            BarricadeManager.onBarricadeSpawned += OnBarricadeSpawned;
            Level.onPostLevelLoaded += OnLevelLoaded;

            if (Level.isLoaded) OnLevelLoaded(0);

            Logger.Log("[RealRadiostation] Плагин загружен. Режим расширенного дебага включен.");
        }

        private void OnLevelLoaded(int level)
        {
            ActiveStations.Clear();
            if (BarricadeManager.regions == null) return;

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
            Logger.Log($"[DEBUG] Карта готова. Станций в реестре: {ActiveStations.Count}");
        }

        private void OnBarricadeSpawned(BarricadeRegion region, BarricadeDrop drop)
        {
            if (drop.asset.id == Configuration.Instance.RadioBarricadeId)
            {
                Logger.Log($"[DEBUG] Новая станция установлена. ID: {drop.instanceID}");
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
            Logger.Log($"[DEBUG] Станция инициализирована: Волна {comp.Frequency}, Режим {comp.Mode}");
        }

        public string GetPosHash(Vector3 pos) => $"{pos.x:F1}_{pos.y:F1}_{pos.z:F1}";

        private void OnRelayVoice(PlayerVoice speaker, bool wantsToUseRadio, ref bool shouldAllow, ref bool shouldBroadcastOverRadio, ref float spatialBlend)
        {
            if (speaker?.player == null) return;

            // Если игрок говорит просто в пространство (не через предмет-рацию)
            if (!wantsToUseRadio)
            {
                var station = GetNearestStation(speaker.player.transform.position, Configuration.Instance.TransmitRadius);
                
                if (station != null && station.Mode == RadioMode.TransmitAndListen)
                {
                    string pName = speaker.player.channel.owner.playerID.characterName;
                    Logger.Log($"[DEBUG VOICE] ПЕРЕХВАТ: Игрок {pName} вещает через стационарное радио на частоте {station.Frequency}");
                    
                    speaker.player.quests.sendSetRadioFrequency(station.Frequency);
                    shouldAllow = true;
                    shouldBroadcastOverRadio = true;
                    spatialBlend = 0f; // Делаем голос чистым, как в рации
                }
            }
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

        public void SaveAllStations()
        {
            var dict = new Dictionary<string, StationData>();
            foreach (var s in ActiveStations)
            {
                if (s != null) dict[GetPosHash(s.transform.position)] = new StationData { Frequency = s.Frequency, Mode = s.Mode };
            }
            DataStorage.Save(dict);
        }

        protected override void Unload()
        {
            PlayerVoice.onRelayVoice -= OnRelayVoice;
            BarricadeManager.onBarricadeSpawned -= OnBarricadeSpawned;
            Level.onPostLevelLoaded -= OnLevelLoaded;
            ActiveStations.Clear();
            Instance = null;
        }
    }
}
