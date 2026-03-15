using SDG.Unturned;
using UnityEngine;
using Steamworks;

namespace RealRadiostation
{
    public enum RadioMode { ListenOnly, TransmitAndListen }

    public class RadioStationComponent : MonoBehaviour
    {
        public uint Frequency { get; set; } = 0;
        public RadioMode Mode { get; set; } = RadioMode.ListenOnly;

        public void ToggleMode()
        {
            Mode = Mode == RadioMode.ListenOnly ? RadioMode.TransmitAndListen : RadioMode.ListenOnly;
            
            string message = $"Режим радиостанции изменен на: {(Mode == RadioMode.ListenOnly ? "Только прослушивание" : "Прием и передача")}";
            
            // Исправлено: использование CSteamID.Nil и EChatMode.LOCAL для корректной работы API
            ChatManager.say(CSteamID.Nil, message, Color.yellow, EChatMode.LOCAL);
        }
    }
}
