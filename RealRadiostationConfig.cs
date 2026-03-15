using Rocket.API;

namespace RealRadiostation
{
    public class RealRadiostationConfig : IRocketPluginConfiguration
    {
        public ushort RadioBarricadeId;
        public float ListenRadius; // Радиус, на котором слышно радио (например, 20 метров)
        public float TransmitRadius; // Насколько близко нужно стоять, чтобы говорить в него (например, 3 метра)

        public void LoadDefaults()
        {
            RadioBarricadeId = 1234; 
            ListenRadius = 20f; 
            TransmitRadius = 3f; 
        }
    }
}
