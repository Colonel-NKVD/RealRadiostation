using UnityEngine;

namespace RealRadiostation
{
    public class RadioStationComponent : MonoBehaviour
    {
        public uint Frequency;
        public bool CanTransmit;

        private void Awake()
        {
            // ЗАЩИТА: Если плагин еще не загрузился (например, при старте сервера), пропускаем.
            // Плагин сам найдет эту рацию через ScanAllStations() позже.
            if (RealRadiostationPlugin.Instance == null) return;

            if (!RealRadiostationPlugin.Instance.ActiveStations.Contains(this))
            {
                RealRadiostationPlugin.Instance.ActiveStations.Add(this);
                Rocket.Core.Logging.Logger.Log($"[DEBUG - Radio] Рация появилась! Всего в сети: {RealRadiostationPlugin.Instance.ActiveStations.Count}");
            }
        }

        private void OnDestroy()
        {
            if (RealRadiostationPlugin.Instance == null) return;

            RealRadiostationPlugin.Instance.ActiveStations.Remove(this);
            Rocket.Core.Logging.Logger.Log($"[DEBUG - Radio] Рация уничтожена. Осталось в сети: {RealRadiostationPlugin.Instance.ActiveStations.Count}");
        }
    }
}
