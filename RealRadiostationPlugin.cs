using Rocket.Core.Plugins;
using SDG.Unturned;
using UnityEngine;
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

            // Используем официальный API Unturned вместо нестабильного Harmony!
            PlayerVoice.onRelayVoice += OnRelayVoice;
            BarricadeManager.onBarricadeSpawned += OnBarricadeSpawned;
            Level.onPostLevelLoaded += OnLevelLoaded;

            // Если плагин перезагружают на лету (когда карта уже загружена)
            if (Level.isLoaded) OnLevelLoaded(0);

            Logger.Log("[RealRadiostation] Плагин загружен. Используется нативный API (без Harmony)!");
        }

        private void OnLevelLoaded(int level)
        {
            ActiveStations.Clear();
            if (BarricadeManager.regions == null) return;

            int count = 0;
            foreach (var region in BarricadeManager.regions)
            {
                foreach (var drop in region.drops)
                {
                    if (drop.asset.id == Configuration.Instance.RadioBarricadeId)
                    {
                        AddStationComponent(drop);
                        count++;
                    }
                }
            }
            Logger.Log($"[DEBUG] Карта загружена. Найдено радиостанций (ID {Configuration.Instance.RadioBarricadeId}): {count}");
        }

        private void OnBarricadeSpawned(BarricadeRegion region, BarricadeDrop drop)
        {
            if (drop.asset.id == Configuration.Instance.RadioBarricadeId)
            {
                AddStationComponent(drop);
                Logger.Log("[DEBUG] Игрок поставил новую радиостанцию!");
            }
        }

        private void AddStationComponent(BarricadeDrop drop)
        {
            var comp = drop.model.gameObject.GetComponent<RadioStationComponent>();
            if (comp == null) comp = drop.model.gameObject.AddComponent<RadioStationComponent>();

            // Надежный хэш по координатам, который не меняется после рестарта
            string hash = GetPosHash(drop.model.position);
            var savedData = DataStorage.Load();

            if (savedData != null && savedData.ContainsKey(hash))
            {
                comp.Frequency = savedData[hash].Frequency;
                comp.Mode = savedData[hash].Mode;
            }
            else
            {
                comp.Frequency = 111111; // Волна по умолчанию (111.111)
                comp.Mode = RadioMode.TransmitAndListen;
            }

            if (!ActiveStations.Contains(comp)) ActiveStations.Add(comp);
        }

        public string GetPosHash(Vector3 pos) => $"{pos.x:F1}_{pos.y:F1}_{pos.z:F1}";

        // ТОТ САМЫЙ ПЕРЕХВАТЧИК ГОЛОСА
        private void OnRelayVoice(PlayerVoice speaker, bool wantsToUseRadio, ref bool shouldAllow, ref bool shouldBroadcastOverRadio, ref float spatialBlend)
        {
            if (speaker == null || speaker.player == null) return;

            string pName = speaker.player.channel.owner.playerID.characterName;

            // Если игрок говорит просто в пространство (нажал Alt/V, а не рацию)
            if (!wantsToUseRadio)
            {
                var station = GetNearestStation(speaker.player.transform.position, Configuration.Instance.TransmitRadius);
                
                if (station != null)
                {
                    Logger.Log($"[DEBUG VOICE] {pName} говорит в ауре радио (Волна: {station.Frequency}, Режим: {station.Mode})");

                    if (station.Mode == RadioMode.TransmitAndListen)
                    {
                        // Подменяем частоту игрока на частоту станции
                        speaker.player.quests.sendSetRadioFrequency(station.Frequency);
                        
                        // Заставляем игру думать, что игрок говорит в рацию
                        shouldAllow = true;
                        shouldBroadcastOverRadio = true;
                        spatialBlend = 0f; // 0f делает звук "радио-подобным" (везде одинаково громко для тех, кто на волне)
                        
                        Logger.Log($"[DEBUG VOICE] УСПЕХ: Голос перехвачен и отправлен на волну {station.Frequency}!");
                    }
                }
            }
        }

        public RadioStationComponent GetNearestStation(Vector3 position, float radius)
        {
            RadioStationComponent nearest = null;
            float minSqrDist = radius * radius;

            foreach (var station in ActiveStations)
            {
                if (station == null || station.gameObject == null) continue;
                float sqrDist = (position - station.transform.position).sqrMagnitude;
                if (sqrDist < minSqrDist)
                {
                    minSqrDist = sqrDist;
                    nearest = station;
                }
            }
            return nearest;
        }

        public void SaveAllStations()
        {
            var dict = new Dictionary<string, StationData>();
            foreach (var s in ActiveStations)
            {
                if (s != null && s.gameObject != null)
                {
                    dict[GetPosHash(s.transform.position)] = new StationData { Frequency = s.Frequency, Mode = s.Mode };
                }
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
            Logger.Log("[RealRadiostation] Плагин выгружен.");
        }
    }
}
