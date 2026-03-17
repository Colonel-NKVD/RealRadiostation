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
        
        // Список всех раций на карте в реальном времени
        public List<RadioStationComponent> ActiveStations = new List<RadioStationComponent>();

        protected override void Load()
        {
            Instance = this;

            // Подписка на события
            Level.onPostLevelLoaded += OnLevelLoaded;
            BarricadeManager.onBarricadeSpawned += OnBarricadeSpawned;
            PlayerVoice.onRelayVoice += OnRelayVoice;

            // Если плагин загружен "на горячую", сканируем уже стоящие объекты
            if (Level.isLoaded) 
            {
                Logger.Log("[DEBUG] Карта уже загружена. Запускаю первичный поиск раций...");
                ScanAllStations();
            }

            Logger.Log("RealRadiostation [PRO] успешно загружен!");
        }

        private void OnLevelLoaded(int level) 
        {
            Logger.Log($"[DEBUG] Карта загружена (Level {level}). Начинаю сканирование станций...");
            ScanAllStations();
        }

        private void OnBarricadeSpawned(BarricadeRegion region, BarricadeDrop drop)
        {
            if (drop.asset.id == Configuration.Instance.RadioBarricadeId)
            {
                Logger.Log($"[DEBUG] Обнаружен спавн радио-баррикады (ID: {drop.asset.id}). Подключаю компоненты...");
                AttachComponentAndLoadData(drop);
            }
        }

        private void ScanAllStations()
        {
            ActiveStations.Clear();
            if (BarricadeManager.regions == null) 
            {
                Logger.Log("[DEBUG] Ошибка: BarricadeManager.regions пуст.");
                return;
            }

            int foundCount = 0;
            foreach (var region in BarricadeManager.regions)
            {
                foreach (var drop in region.drops)
                {
                    if (drop.asset.id == Configuration.Instance.RadioBarricadeId)
                    {
                        AttachComponentAndLoadData(drop);
                        foundCount++;
                    }
                }
            }
            Logger.Log($"[DEBUG] Сканирование завершено. Найдено раций: {foundCount}");
        }

        private void AttachComponentAndLoadData(BarricadeDrop drop)
        {
            // Берем существующий компонент или добавляем новый
            var comp = drop.model.gameObject.GetComponent<RadioStationComponent>() ?? drop.model.gameObject.AddComponent<RadioStationComponent>();
            
            string hash = GetPosHash(drop.model.position);
            var savedData = DataStorage.Load();

            if (savedData != null && savedData.ContainsKey(hash))
            {
                comp.Frequency = savedData[hash].Frequency;
                comp.CanTransmit = savedData[hash].CanTransmit;
                Logger.Log($"[DEBUG] Данные загружены из файла для рации на {hash}: Freq={comp.Frequency}, Transmit={comp.CanTransmit}");
            }
            else
            {
                comp.Frequency = Configuration.Instance.DefaultFrequency;
                comp.CanTransmit = false; // По умолчанию только слушать
                Logger.Log($"[DEBUG] Новая рация на {hash}. Установлены настройки по умолчанию.");
            }
        }

        // ==========================================
        // ЛОГИКА ТРАНСЛЯЦИИ ГОЛОСА
        // ==========================================
        private void OnRelayVoice(PlayerVoice speaker, bool wantsToUseWalkieTalkie, ref bool shouldAllow, ref bool shouldBroadcastOverRadio, ref PlayerVoice.RelayVoiceCullingHandler cullingHandler)
        {
            uint currentBroadcastFreq = speaker.player.quests.radioFrequency;
            bool isBroadcastingToRadio = false;

            // 1. Проверяем, говорит ли игрок через нашу стационарную рацию
            var speakerStation = GetNearestStation(speaker.transform.position, Configuration.Instance.ListenAuraRadius, true);
            
            if (speakerStation != null)
            {
                Logger.Log($"[DEBUG] Игрок {speaker.player.channel.owner.playerID.characterName} вещает через СТАЦИОНАРНУЮ рацию (Freq: {speakerStation.Frequency})");
                isBroadcastingToRadio = true;
                currentBroadcastFreq = speakerStation.Frequency;
                shouldAllow = true;
                shouldBroadcastOverRadio = true; 
            }
            else if (wantsToUseWalkieTalkie && speaker.hasUseableWalkieTalkie)
            {
                Logger.Log($"[DEBUG] Игрок {speaker.player.channel.owner.playerID.characterName} вещает через ОБЫЧНУЮ рацию (Freq: {currentBroadcastFreq})");
                isBroadcastingToRadio = true;
            }

            // 2. Настраиваем слушателей
            if (isBroadcastingToRadio)
            {
                uint targetFreq = currentBroadcastFreq;

                cullingHandler = (PlayerVoice spk, PlayerVoice lst) =>
                {
                    // А. Обычная дистанция
                    if (PlayerVoice.handleRelayVoiceCulling_Proximity(spk, lst)) return true;

                    // Б. Слушатель с карманной рацией
                    if (lst.canHearRadio && lst.player.quests.radioFrequency == targetFreq) return true;

                    // В. Слушатель в радиусе нашей баррикады
                    var listenerStation = GetNearestStation(lst.transform.position, Configuration.Instance.ListenAuraRadius, false);
                    if (listenerStation != null && listenerStation.Frequency == targetFreq) 
                    {
                        return true;
                    }

                    return false;
                };
            }
        }

        // ==========================================
        // УТИЛИТЫ
        // ==========================================
        public RadioStationComponent GetNearestStation(Vector3 pos, float maxRadius, bool requireTransmitMode)
        {
            RadioStationComponent nearest = null;
            float minSqrDist = maxRadius * maxRadius;

            foreach (var station in ActiveStations)
            {
                if (requireTransmitMode && !station.CanTransmit) continue;
                
                float sqrDist = (station.transform.position - pos).sqrMagnitude;
                if (sqrDist <= minSqrDist)
                {
                    minSqrDist = sqrDist;
                    nearest = station;
                }
            }
            return nearest;
        }

        public string GetPosHash(Vector3 pos) => $"{pos.x:F1}_{pos.y:F1}_{pos.z:F1}";

        public void SaveAllStations()
        {
            var dict = new Dictionary<string, StationData>();
            foreach (var s in ActiveStations)
            {
                dict[GetPosHash(s.transform.position)] = new StationData { Frequency = s.Frequency, CanTransmit = s.CanTransmit };
            }
            DataStorage.Save(dict);
            Logger.Log($"[DEBUG] Состояние {ActiveStations.Count} раций сохранено в файл.");
        }

        protected override void Unload()
        {
            Level.onPostLevelLoaded -= OnLevelLoaded;
            BarricadeManager.onBarricadeSpawned -= OnBarricadeSpawned;
            PlayerVoice.onRelayVoice -= OnRelayVoice;
            Instance = null;
            Logger.Log("RealRadiostation выгружен.");
        }
    }
}
