using HarmonyLib;
using SDG.Unturned;

namespace RealRadiostation
{
    // Указываем Harmony, какой именно метод игры мы хотим "взломать"
    [HarmonyPatch(typeof(PlayerVoice), "askVoiceChat")]
    public static class VoicePatch
    {
        // Метод Prefix выполняется ДО оригинального кода игры
        // Ключевое слово "ref" позволяет нам подменить параметр на лету
        public static void Prefix(PlayerVoice __instance, ref bool wantsToUseRadio)
        {
            // Если игрок УЖЕ держит переносную рацию в руках, плагин не вмешивается
            if (wantsToUseRadio) return; 

            // Ищем стационарную радиостанцию в радиусе 5 метров
            var station = RealRadiostationPlugin.Instance.GetNearestStation(__instance.player.transform.position, 5f);

            if (station != null && station.Mode == RadioMode.TransmitAndListen)
            {
                // 1. Принудительно настраиваем рацию игрока на волну станции
                if (__instance.player.quests.radioFrequency != station.Frequency)
                {
                    __instance.player.quests.sendSetRadioFrequency(station.Frequency);
                }

                // 2. ГЛАВНАЯ МАГИЯ: Мы обманываем игру, заставляя ее думать, 
                // что игрок говорит в рацию, даже если ее нет в руках.
                wantsToUseRadio = true; 
            }
        }
    }
}
