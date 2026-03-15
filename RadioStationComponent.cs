using SDG.Unturned;
using UnityEngine;
using Steamworks;

namespace RealRadiostation
{
    public enum RadioMode { ListenOnly, TransmitAndListen }

    public class RadioStationComponent : MonoBehaviour
    {
        public float Frequency { get; set; } = 0f;
        public RadioMode Mode { get; set; } = RadioMode.ListenOnly;

        public void ToggleMode()
        {
            Mode = Mode == RadioMode.ListenOnly ? RadioMode.TransmitAndListen : RadioMode.ListenOnly;
            
            string message = $"Режим радиостанции: {(Mode == RadioMode.ListenOnly ? "Только прием" : "Прием и передача")}";
            ChatManager.say(CSteamID.Nil, message, Color.yellow, EChatMode.LOCAL);
        }

        // Защита от утечек памяти: удаляем станцию из кэша при её уничтожении
        private void OnDestroy()
        {
            if (RealRadiostationPlugin.Instance != null && RealRadiostationPlugin.Instance.ActiveStations.Contains(this))
            {
                RealRadiostationPlugin.Instance.ActiveStations.Remove(this);
            }
        }
    }
}
