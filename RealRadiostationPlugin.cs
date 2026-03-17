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
        
        public string PluginVersion = "2.0-PRO";

        protected override void Load()
        {
            Instance = this;
            Logger.Log($"--- [RealRadiostation] ВЕРСИЯ: {PluginVersion} ---");
            Logger.Log("[RealRadiostation] Загрузка профессиональной архитектуры...");

            // 1. Подписываемся на события загрузки карты и спавна объектов
            Level.onPostLevelLoaded += OnLevelLoaded;
            BarricadeManager.onBarricadeSpawned += OnBarricadeSpawned;
            
            // 2. ПОДПИСЫВАЕМСЯ НА ОФИЦИАЛЬНЫЙ КАНАЛ ГОЛОСА UNTURNED
            PlayerVoice.onRelayVoice += OnRelayVoice;

            if (Level.isLoaded) ScanStations();

            Logger.Log("[RealRadiostation] Плагин успешно интегрирован в ядро игры.");
        }

        // ==========================================
        // ЯДРО ПЛАГИНА: ПЕРЕХВАТ И МАРШРУТИЗАЦИЯ ГОЛОСА
        // ==========================================
        private void OnRelayVoice(PlayerVoice sender, bool wantsToUseRadio, ref bool shouldAllow, ref bool shouldBroadcastOverRadio, ref float spatialBlend)
        {
            // Если игрок уже говорит в рацию (держит её в руках), не вмешиваемся
            if (wantsToUseRadio) return; 

            // Проверяем, есть ли рядом с говорящим стационарная радиостанция (радиус 5 метров)
            var station = GetNearestStation(sender.player.transform.position, 5f);

            if (station != null && station.Mode == RadioMode.TransmitAndListen)
            {
                // 1. Принудительно переключаем частоту игрока на частоту станции
                if (sender.player.quests.radioFrequency != station.Frequency)
                {
                    sender.player.quests.sendSetRadioFrequency(station.Frequency);
                }

                // 2. МАГИЯ: Приказываем серверу транслировать этот локальный голос на всю карту по радио!
                shouldBroadcastOverRadio = true;

                // Для отладки (потом можно закомментировать)
                Logger.Log($"[ЭФИР] Игрок {sender.player.channel.owner.playerID.characterName} вещает через стационарную рацию на волне {station.Frequency}!");
            }
        }

        // ==========================================
        // МЕНЕДЖМЕНТ СТАНЦИЙ
        // ==========================================
        private void OnLevelLoaded(int level) => ScanStations();
        
        private void OnBarricadeSpawned(BarricadeRegion region, BarricadeDrop drop) 
        { 
            if (drop.asset.id == 1466) ScanStations(); 
        }

        public void ScanStations()
        {
            ActiveStations.Clear();
            if (BarricadeManager.regions == null) return;
            
            var savedData = DataStorage.Load();

            foreach (var region in BarricadeManager.regions)
            {
                foreach (var drop in region.drops)
                {
                    if (drop.asset.id == 1466)
                    {
                        var comp = drop.model.gameObject.GetComponent<RadioStationComponent>() 
                                   ?? drop.model.gameObject.AddComponent<RadioStationComponent>();
                        
                        string hash = GetPosHash(drop.model.position);
                        
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
                    }
                }
            }
            Logger.Log($"[Система] Зарегистрировано стационарных раций: {ActiveStations.Count}");
        }

        public string GetPosHash(Vector3 pos) => $"{pos.x:F1}_{pos.y:F1}_{pos.z:F1}";

        public void SaveAllStations()
        {
            var dict = new Dictionary<string, StationData>();
            foreach (var s in ActiveStations)
            {
                if (s != null) dict[GetPosHash(s.transform.position)] = new StationData { Frequency = s.Frequency, Mode = s.Mode };
            }
            DataStorage.Save(dict);
        }

        public RadioStationComponent GetNearestStation(Vector3 pos, float radius)
        {
            RadioStationComponent nearest = null;
            float minSqrDist = radius * radius;
            
            foreach (var s in ActiveStations)
            {
                if (s == null) continue;
                float sqrDist = (s.transform.position - pos).sqrMagnitude;
                if (sqrDist < minSqrDist)
                {
                    minSqrDist = sqrDist;
                    nearest = s;
                }
            }
            return nearest;
        }

        protected override void Unload()
        {
            // Отписываемся от всех событий при выключении плагина
            Level.onPostLevelLoaded -= OnLevelLoaded;
            BarricadeManager.onBarricadeSpawned -= OnBarricadeSpawned;
            PlayerVoice.onRelayVoice -= OnRelayVoice;
            
            ActiveStations.Clear();
            Instance = null;
        }
    }
}
