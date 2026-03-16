using HarmonyLib;
using SDG.Unturned;
using UnityEngine;

namespace RealRadiostation
{
    public static class VoicePatch
    {
        public static bool Prefix(PlayerVoice __instance, object[] __args)
        {
            // Unturned параметры обычно идут так: 
            // [0] wantsToUseRadio (bool)
            // [1] shouldAllow (ref bool)
            // [2] shouldBroadcastOverRadio (ref bool)
            
            bool wantsToUseRadio = (bool)__args[0];

            if (!wantsToUseRadio)
            {
                var plugin = RealRadiostationPlugin.Instance;
                if (plugin == null || __instance.player == null) return true;

                var station = plugin.GetNearestStation(__instance.player.transform.position, plugin.Configuration.Instance.TransmitRadius);
                
                if (station != null && station.Mode == RadioMode.TransmitAndListen)
                {
                    // Устанавливаем частоту игроку
                    __instance.player.quests.sendSetRadioFrequency(station.Frequency);
                    
                    // Перезаписываем параметры в массиве аргументов
                    __args[1] = true; // shouldAllow
                    __args[2] = true; // shouldBroadcastOverRadio
                    
                    // Если есть 4-й параметр (spatialBlend), ставим его в 0 (радио-режим)
                    if (__args.Length > 3) __args[3] = 0f;

                    Rocket.Core.Logging.Logger.Log($"[DEBUG VOICE] Голос перехвачен радиостанцией! Частота: {station.Frequency}");
                }
            }
            return true;
        }
    }
}
