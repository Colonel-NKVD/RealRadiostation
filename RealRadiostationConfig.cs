using Rocket.API;

namespace RealRadiostation
{
    public class RealRadiostationConfig : IRocketPluginConfiguration
    {
        public ushort RadioBarricadeId;
        public float ListenAuraRadius;
        public float SetupCommandRadius;
        public uint DefaultFrequency;

        public void LoadDefaults()
        {
            RadioBarricadeId = 1234;     // Замени на ID своей баррикады
            ListenAuraRadius = 20f;      // Дальность ауры, внутри которой работает рация
            SetupCommandRadius = 3f;     // Дальность, с которой можно настроить рацию через команду
            DefaultFrequency = 333333;   // Частота по умолчанию при установке
        }
    }
}
