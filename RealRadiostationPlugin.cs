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

            // Подписка на загрузку карты и динамический спавн (когда игрок ставит объект)
            Level.onPostLevelLoaded += OnLevelLoaded;
            BarricadeManager.onBarricadeSpawned += OnBarricadeSpawned;

            // Нативный перехват голоса из движка Unturned
            PlayerVoice.onRelayVoice += OnRelayVoice;

            // Если плагин загружен "на горячую", сканируем уже стоящие объекты
            if (Level.isLoaded) ScanAllStations();

            Logger.Log("RealRadiostation [PRO] успешно загружен!");
        }

        private void OnLevelLoaded(int level) => ScanAllStations();

        private void OnBarricadeSpawned(BarricadeRegion region, BarricadeDrop drop)
        {
            if (drop.asset.id == Configuration.Instance.RadioBarricadeId)
            {
                AttachComponentAndLoadData(drop);
            }
        }

        private void ScanAllStations()
        {
            ActiveStations.Clear();
            if (BarricadeManager.regions == null) return;

            foreach (var region in BarricadeManager.regions)
            {
                foreach (var drop in region.drops)
                {
                    if (drop.asset.id == Configuration.Instance.RadioBarricadeId)
                    {
                        AttachComponentAndLoadData(drop);
                    }
                }
            }
        }

        private void AttachComponentAndLoadData(BarricadeDrop drop)
        {
            var comp = drop.model.gameObject.GetComponent<RadioStationComponent>() ?? drop.model.gameObject.AddComponent<RadioStationComponent>();
            
            string hash = GetPosHash(drop.model.position);
            var savedData = DataStorage.Load();

            if (savedData.ContainsKey(hash))
            {
                comp.Frequency = savedData[hash].Frequency;
                comp.CanTransmit = savedData[hash].CanTransmit;
            }
            else
            {
                comp.Frequency = Configuration.Instance.DefaultFrequency;
                comp.CanTransmit = false; // По умолчанию только слушать
            }
        }

        // ==========================================
        // ЛОГИКА ТРАНСЛЯЦИИ ГОЛОСА (АБСОЛЮТНЫЙ ДЕБАГ)
        // ==========================================
        private void OnRelayVoice(PlayerVoice speaker, bool wantsToUseWalkieTalkie, ref bool shouldAllow, ref bool shouldBroadcastOverRadio, ref PlayerVoice.RelayVoiceCullingHandler cullingHandler)
        {
            // 1. БАЗОВЫЙ ДЕБАГ: Проверяем, что событие вообще срабатывает
            Logger.Log($"[VOICE_TEST] Сервер получил голос от {speaker.player.channel.owner.playerID.characterName}");

            uint currentBroadcastFreq = speaker.player.quests.radioFrequency;
            bool isBroadcastingToRadio = false;

            // 2. ДЕБАГ РАЦИЙ: Ищем ЛЮБУЮ рацию рядом, просто чтобы понять, видит ли её плагин
            var anyStationNear = GetNearestStation(speaker.transform.position, Configuration.Instance.ListenAuraRadius, false);
            if (anyStationNear != null)
            {
                Logger.Log($"[VOICE_TEST] Рядом с игроком есть рация! Её режим CanTransmit: {anyStationNear.CanTransmit}");
            }

            // 3. ОСНОВНАЯ ЛОГИКА: Ищем рацию, в которую МОЖНО говорить
            var speakerStation = GetNearestStation(speaker.transform.position, Configuration.Instance.ListenAuraRadius, true);
            
            if (speakerStation != null)
            {
                Logger.Log($"[VOICE_TEST] УСПЕХ! Игрок вещает через СТАЦИОНАРНУЮ рацию (Freq: {speakerStation.Frequency})");
                isBroadcastingToRadio = true;
                currentBroadcastFreq = speakerStation.Frequency;
                shouldAllow = true;
                shouldBroadcastOverRadio = true; // Заставляем движок включить радио-режим
            }
            else if (wantsToUseWalkieTalkie && speaker.hasUseableWalkieTalkie)
            {
                Logger.Log($"[VOICE_TEST] Игрок вещает через ОБЫЧНУЮ карманную рацию.");
                isBroadcastingToRadio = true;
            }
            else
            {
                 Logger.Log($"[VOICE_TEST] Обычный локальный голос. В эфир не идет.");
            }

            // 4. Настраиваем слушателей (кто это услышит)
            if (isBroadcastingToRadio)
            {
                uint targetFreq = currentBroadcastFreq;

                cullingHandler = (PlayerVoice spk, PlayerVoice lst) =>
                {
                    // А. Обычная дистанция (слышно голос рядом в любом случае)
                    if (PlayerVoice.handleRelayVoiceCulling_Proximity(spk, lst)) return true;

                    // Б. Слушатель с карманной рацией на нужной волне
                    if (lst.canHearRadio && lst.player.quests.radioFrequency == targetFreq) return true;

                    // В. Слушатель без рации, но в радиусе нашей стационарной баррикады
                    var listenerStation = GetNearestStation(lst.transform.position, Configuration.Instance.ListenAuraRadius, false);
                    if (listenerStation != null && listenerStation.Frequency == targetFreq) return true;

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
        }

        protected override void Unload()
        {
            Level.onPostLevelLoaded -= OnLevelLoaded;
            BarricadeManager.onBarricadeSpawned -= OnBarricadeSpawned;
            PlayerVoice.onRelayVoice -= OnRelayVoice;
            Instance = null;
        }
    }
}
