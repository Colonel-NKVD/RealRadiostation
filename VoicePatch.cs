using HarmonyLib;
using SDG.Unturned;
using UnityEngine;

namespace RealRadiostation
{
    // Nelson (разработчик Unturned) в новых версиях переименовал метод. 
    // Пробуем пропатчить handleRelayVoiceInternal
    [HarmonyPatch(typeof(PlayerVoice), "handleRelayVoiceInternal")]
    public static class VoicePatch
    {
        [HarmonyPrefix]
        public static bool Prefix(PlayerVoice __instance, bool wantsToUseRadio, ref bool shouldAllow, ref bool shouldBroadcastOverRadio)
        {
            if (!wantsToUseRadio)
            {
                var plugin = RealRadiostationPlugin.Instance;
                if (plugin == null || __instance.player == null) return true;

                // Проверяем наличие радиостанции рядом
                var station = plugin.GetNearestStation(__instance.player.transform.position, plugin.Configuration.Instance.TransmitRadius);
                
                if (station != null && station.Mode == RadioMode.TransmitAndListen)
                {
                    // Настраиваем рацию игрока на частоту станции
                    __instance.player.quests.sendSetRadioFrequency((uint)station.Frequency);
                    
                    shouldAllow = true;
                    shouldBroadcastOverRadio = true; 
                }
            }
            return true; // Разрешаем игре выполнить остальную логику
        }
    }
}
