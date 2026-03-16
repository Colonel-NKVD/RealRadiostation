using HarmonyLib;
using SDG.Unturned;
using UnityEngine;

namespace RealRadiostation
{
    // Меняем "handleRelayVoiceInternal" на "receiveRelayVoice"
    [HarmonyPatch(typeof(PlayerVoice), "receiveRelayVoice")]
    public static class VoicePatch
    {
        [HarmonyPrefix]
        public static void Prefix(PlayerVoice __instance, bool wantsToUseRadio, ref bool shouldAllow, ref bool shouldBroadcastOverRadio, ref float spatialBlend)
        {
            // Если игрок говорит просто голосом (не в рацию)
            if (!wantsToUseRadio)
            {
                var plugin = RealRadiostationPlugin.Instance;
                if (plugin == null || __instance.player == null) return;

                var playerPos = __instance.player.transform.position;
                
                // Ищем станцию в радиусе, указанном в конфиге
                var station = plugin.GetNearestStation(playerPos, plugin.Configuration.Instance.TransmitRadius);
                
                if (station != null && station.Mode == RadioMode.TransmitAndListen)
                {
                    // Устанавливаем частоту
                    uint freq = (uint)station.Frequency;
                    __instance.player.quests.sendSetRadioFrequency(freq);
                    
                    // Разрешаем передачу и включаем эффект рации
                    shouldAllow = true;
                    shouldBroadcastOverRadio = true;
                    
                    // Опционально: делаем звук 2D (0f) или 3D (1f), если нужно
                    // spatialBlend = 0f; 
                }
            }
        }
    }
}
