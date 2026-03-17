using UnityEngine;

namespace RealRadiostation
{
    public class RadioStationComponent : MonoBehaviour
    {
        public uint Frequency;
        public bool CanTransmit;

        private void Awake()
        {
            // При спавне или загрузке карты добавляем себя в список активных раций
            if (!RealRadiostationPlugin.Instance.ActiveStations.Contains(this))
            {
                RealRadiostationPlugin.Instance.ActiveStations.Add(this);
            }
        }

        private void OnDestroy()
        {
            // Умная очистка: когда баррикаду ломают, она сама удаляется из системы
            RealRadiostationPlugin.Instance.ActiveStations.Remove(this);
        }
    }
}
