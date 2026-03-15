using SDG.Unturned;
using UnityEngine;
using Steamworks;

namespace RealRadiostation
{
    public enum RadioMode { ListenOnly, TransmitAndListen }

    public class RadioStationComponent : MonoBehaviour
    {
        // Исправлено: тип изменен на float для поддержки частот с точкой
        public float Frequency { get; set; } = 0f;
        public RadioMode Mode { get; set; } = RadioMode.ListenOnly;

        public void ToggleMode()
        {
            Mode = Mode == RadioMode.ListenOnly ? RadioMode.TransmitAndListen : RadioMode.ListenOnly;
            
            string message = $"Режим радиостанции изменен на: {(Mode == RadioMode.ListenOnly ? "Только прослушивание" : "Прием и передача")}";
            ChatManager.say(CSteamID.Nil, message, Color.yellow, EChatMode.LOCAL);
        }
    }
}
