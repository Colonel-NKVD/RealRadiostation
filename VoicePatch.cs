using HarmonyLib;
using SDG.Unturned;
using UnityEngine;

namespace RealRadiostation
{
    // Патчим основной метод передачи голоса
    [HarmonyPatch(typeof(PlayerVoice), "receiveRelayVoice")]
    public static class VoicePatch
    {
        [HarmonyPrefix]
        public static bool Prefix(PlayerVoice __instance, bool wantsToUseRadio, ref bool shouldAllow, ref bool shouldBroadcastOverRadio)
        {
            // Если игрок говорит обычным голосом (не нажал кнопку рации)
            if (!wantsToUseRadio)
            {
                var plugin = RealRadiostationPlugin.Instance;
                if (plugin == null || __instance.player == null) return true;

                // Проверяем наличие радиостанции рядом
                var station = plugin.GetNearestStation(__instance.player.transform.position, plugin.Configuration.Instance.TransmitRadius);
                
                if (station != null && station.Mode == RadioMode.TransmitAndListen)
                {
                    // Симулируем передачу через рацию на частоте станции
                    __instance.player.quests.sendSetRadioFrequency((uint)station.Frequency);
                    
                    shouldAllow = true;
                    shouldBroadcastOverRadio = true; 
                }
            }
            return true; // Продолжаем выполнение оригинального метода
        }
    }
}
