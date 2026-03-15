using SDG.Unturned;
using UnityEngine;

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
            ChatManager.say(transform.position, $"Режим радиостанции изменен на: {(Mode == RadioMode.ListenOnly ? "Только прослушивание" : "Прием и передача")}", Color.yellow, 10f, false);
        }
    }
}
