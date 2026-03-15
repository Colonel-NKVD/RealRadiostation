using Rocket.Core.Plugins;
using SDG.Unturned;
using UnityEngine;
using System.Collections.Generic;

namespace RealRadiostation
{
    public class RealRadiostationPlugin : RocketPlugin<RealRadiostationConfig>
    {
        public static RealRadiostationPlugin Instance;
        // Список для быстрого доступа из команд
        public List<RadioStationComponent> ActiveStations = new List<RadioStationComponent>();

        protected override void Load()
        {
            Instance = this;
            BarricadeManager.onBarricadeSpawned += OnBarricadeSpawned;
            PlayerAnimator.OnPoint_Global += OnPlayerPoint;
            
            // Нативная обработка голоса
            VOIPChannel.OnVoiceChatRelay += OnVoiceRelay;
        }

        private void OnVoiceRelay(Player speaker, uint frequency, ref bool shouldAllow)
        {
            foreach (var station in ActiveStations)
            {
                // Если игрок говорит в рацию на частоте станции и станция в режиме приема
                if (station.Frequency == frequency && station.Mode == RadioMode.TransmitAndListen)
                {
                    float dist = Vector3.Distance(speaker.transform.position, station.transform.position);
                    if (dist <= Configuration.Instance.BroadcastRadius)
                    {
                        // Включаем "громкую связь" для всех вокруг станции
                        BroadcastToNearby(station, speaker);
                    }
                }
            }
        }

        private void OnPlayerPoint(PlayerAnimator animator)
        {
            // Рейкаст для переключения режима по анимации Point (на клавишу по умолчанию)
            if (Physics.Raycast(animator.player.look.aim.position, animator.player.look.aim.forward, out RaycastHit hit, 3f, RayMasks.BARRICADE))
            {
                var component = hit.transform.GetComponent<RadioStationComponent>();
                if (component != null)
                {
                    component.ToggleMode();
                }
            }
        }

        private void OnBarricadeSpawned(BarricadeRegion region, BarricadeDrop drop)
        {
            if (drop.asset.id == Configuration.Instance.RadioBarricadeId)
            {
                var comp = drop.model.gameObject.AddComponent<RadioStationComponent>();
                ActiveStations.Add(comp);
            }
        }

        private void BroadcastToNearby(RadioStationComponent station, Player speaker)
        {
            // Логика: если кто-то говорит в рацию на нужной частоте, 
            // мы можем воспроизвести этот звук через эффект или напрямую через API слушателей.
        }

        // --- ПАТЧ ДЛЯ СОХРАНЕНИЯ И ПОИСКА ---

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

        public RadioStationComponent GetNearestStation(Vector3 position)
        {
            RadioStationComponent nearest = null;
            float minDir = 3f;
            foreach (var station in ActiveStations)
            {
                float d = Vector3.Distance(position, station.transform.position);
                if (d < minDir)
                {
                    minDir = d;
                    nearest = station;
                }
            }
            return nearest;
        }

        // ------------------------------------

        protected override void Unload()
        {
            BarricadeManager.onBarricadeSpawned -= OnBarricadeSpawned;
            ActiveStations.Clear();
        }
    }
}
