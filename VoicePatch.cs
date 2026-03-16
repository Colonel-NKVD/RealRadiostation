using HarmonyLib;
using SDG.Unturned;
using UnityEngine;

namespace RealRadiostation
{
    public static class VoicePatch
    {
        // Используем object[] __args, чтобы патч подошел к любой версии Unturned
        public static bool Prefix(PlayerVoice __instance, object[] __args)
        {
            // В Unturned параметры обычно такие: 0: wantsToUseRadio, 1: shouldAllow, 2: shouldBroadcast
            // Мы безопасно достаем первый параметр (нажата ли кнопка рации)
            bool wantsToUseRadio = (bool)__args[0];

            if (!wantsToUseRadio)
            {
                var plugin = RealRadiostationPlugin.Instance;
                if (plugin == null || __instance.player == null) return true;

                var station = plugin.GetNearestStation(__instance.player.transform.position, plugin.Configuration.Instance.TransmitRadius);
                
                if (station != null && station.Mode == RadioMode.TransmitAndListen)
                {
                    __instance.player.quests.sendSetRadioFrequency((uint)station.Frequency);
                    
                    // Меняем значения параметров "разрешить" и "вещать через рацию" в массиве аргументов
                    __args[1] = true; // shouldAllow
                    __args[2] = true; // shouldBroadcastOverRadio
                }
            }
            return true;
        }
    }
}
