using Rocket.API;

namespace RealRadiostation
{
    public class RealRadiostationConfig : IRocketPluginConfiguration
    {
        public ushort RadioBarricadeId { get; set; }
        public float BroadcastRadius { get; set; }

        // Исправлено: метод переименован в LoadDefaults для соответствия интерфейсу IDefaultable
        public void LoadDefaults()
        {
            RadioBarricadeId = 12345;
            BroadcastRadius = 40f;
        }
    }
}
