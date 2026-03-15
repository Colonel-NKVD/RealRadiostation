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
            
            // Подключаемся к глубокому API маршрутизации голоса Unturned
            PlayerVoice.onRelayVoice += OnVoiceRelay;
        }

        private void OnVoiceRelay(PlayerVoice speaker, bool wantsToUseRadio, ref bool shouldAllow, ref bool shouldBroadcastOverRadio, ref PlayerVoice.VoiceRouting routing)
        {
            if (speaker == null || speaker.player == null) return;

            Player spkPlayer = speaker.player;
            RadioStationComponent transmittingStation = null;

            // Если игрок говорит просто голосом (не в личную рацию), проверяем, есть ли рядом стационарная рация
            if (!wantsToUseRadio)
            {
                transmittingStation = GetNearestStation(spkPlayer.transform.position, Configuration.Instance.TransmitRadius);
                
                if (transmittingStation != null && transmittingStation.Mode == RadioMode.TransmitAndListen)
                {
                    // Включаем характерный эффект рации ("пшш") для атмосферы
                    shouldBroadcastOverRadio = true; 
                }
            }

            // Внедряем нашу логику в цепочку маршрутизации пакетов
            routing += (PlayerVoice spk, List<SteamPlayer> listeners) =>
            {
                float activeFrequency = 0f;
                bool isBroadcasting = false;

                // Сценарий 1: Игрок говорит в ручную рацию
                if (wantsToUseRadio)
                {
                    activeFrequency = spkPlayer.quests.radioFrequency;
                    isBroadcasting = true;
                }
                // Сценарий 2: Игрок говорит локально, но рядом с баррикадой передачи
                else if (transmittingStation != null && transmittingStation.Mode == RadioMode.TransmitAndListen)
                {
                    activeFrequency = transmittingStation.Frequency;
                    isBroadcasting = true;
                }

                // Если передача идет в эфир, ищем слушателей
                if (isBroadcasting && activeFrequency > 0f)
                {
                    float listenRadiusSqr = Configuration.Instance.ListenRadius * Configuration.Instance.ListenRadius;

                    foreach (var station in ActiveStations)
                    {
                        if (station == null) continue;

                        // Сверяем частоты (с учетом погрешности float)
                        if (Mathf.Abs(station.Frequency - activeFrequency) < 0.001f)
                        {
                            foreach (var client in Provider.clients)
                            {
                                // Пропускаем говорящего и тех, кто уже есть в списке слушателей
                                if (client.player == spkPlayer || listeners.Contains(client)) continue;

                                // Оптимизированная проверка дистанции (через квадраты)
                                float sqrDist = (client.player.transform.position - station.transform.position).sqrMagnitude;
                                if (sqrDist <= listenRadiusSqr)
                                {
                                    listeners.Add(client);
                                }
                            }
                        }
                    }
                }
            };
        }

        private void OnBarricadeSpawned(BarricadeRegion region, BarricadeDrop drop)
        {
            if (drop.asset.id == Configuration.Instance.RadioBarricadeId)
            {
                var comp = drop.model.gameObject.AddComponent<RadioStationComponent>();
                ActiveStations.Add(comp);
            }
        }

        // Добавлен параметр maxDistance для гибкого поиска
        public RadioStationComponent GetNearestStation(Vector3 position, float maxDistance = 3f)
        {
            RadioStationComponent nearest = null;
            float minDistanceSqr = maxDistance * maxDistance;

            foreach (var station in ActiveStations)
            {
                if (station == null) continue;

                float sqrDist = (position - station.transform.position).sqrMagnitude;
                if (sqrDist < minDistanceSqr)
                {
                    minDistanceSqr = sqrDist;
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
                    dataToSave[drop.instanceID] = new StationData 
                    { 
                        Frequency = station.Frequency, 
                        Mode = station.Mode 
                    };
                }
            }
            DataStorage.Save(dataToSave);
        }

        protected override void Unload()
        {
            BarricadeManager.onBarricadeSpawned -= OnBarricadeSpawned;
            PlayerVoice.onRelayVoice -= OnVoiceRelay; // Обязательная отписка
            ActiveStations.Clear();
        }
    }
}
