using Rocket.API;

namespace RealRadiostation
{
    public class RealRadiostationConfig : IRocketPluginConfiguration
    {
        public ushort RadioBarricadeId { get; set; }
        public float BroadcastRadius { get; set; }
        public void Defaults()
        {
            RadioBarricadeId = 12345; // Замените на ID вашей баррикады
            BroadcastRadius = 40f;
        }
    }
}
