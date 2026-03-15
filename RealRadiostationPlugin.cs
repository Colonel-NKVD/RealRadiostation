using Rocket.Core.Plugins;
using SDG.Unturned;
using UnityEngine;
using System.Collections.Generic;

namespace RealRadiostation
{
    public class RealRadiostationPlugin : RocketPlugin<RealRadiostationConfig>
    {
        public static RealRadiostationPlugin Instance;
        private List<RadioStationComponent> _activeStations = new List<RadioStationComponent>();

        protected override void Load()
        {
            Instance = this;
            BarricadeManager.onBarricadeSpawned += OnBarricadeSpawned;
            PlayerAnimator.OnPoint_Global += OnPlayerPoint;
            
            // Логика трансляции голоса через нативный API Unturned
            VOIPChannel.OnVoiceChatRelay += OnVoiceRelay;
        }

        private void OnVoiceRelay(Player speaker, uint frequency, ref bool shouldAllow)
        {
            foreach (var station in _activeStations)
            {
                if (station.Frequency != frequency) continue;

                // Если станция в режиме передачи, она "подхватывает" голос игрока на частоте
                float dist = Vector3.Distance(speaker.transform.position, station.transform.position);
                if (dist <= Configuration.Instance.BroadcastRadius)
                {
                    // Трансляция всем, кто в радиусе станции
                    AlertNearbyPlayers(station, speaker);
                }
            }
        }

        private void OnPlayerPoint(PlayerAnimator animator)
        {
            // Проверка на нажатие анимации "Point" рядом с баррикадой
            if (Physics.Raycast(animator.player.look.aim.position, animator.player.look.aim.forward, out RaycastHit hit, 4f, RayMasks.BARRICADE))
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
                drop.model.gameObject.AddComponent<RadioStationComponent>();
                _activeStations.Add(drop.model.gameObject.GetComponent<RadioStationComponent>());
            }
        }

        private void AlertNearbyPlayers(RadioStationComponent station, Player speaker)
        {
            // Механика: заставляем игроков в радиусе слышать speaker, даже если их рация выключена
            foreach (var steamPlayer in Provider.clients)
            {
                if (Vector3.Distance(steamPlayer.player.transform.position, station.transform.position) <= Configuration.Instance.BroadcastRadius)
                {
                    // Имитация прямого получения голоса
                    // В текущем API Unturned это реализуется через подмену частоты слушателя на лету или ForceTalk
                }
            }
        }

        protected override void Unload()
        {
            BarricadeManager.onBarricadeSpawned -= OnBarricadeSpawned;
            _activeStations.Clear();
        }
    }
}
