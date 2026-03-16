using HarmonyLib;
using SDG.Unturned;
using UnityEngine;

namespace RealRadiostation
{
    // Патчим метод, который вызывается игрой при каждой попытке передать кусок голоса
    [HarmonyPatch(typeof(PlayerVoice), "handleRelayVoiceInternal")]
    public static class VoicePatch
    {
        [HarmonyPrefix]
        public static void Prefix(PlayerVoice __instance, bool wantsToUseRadio, ref bool shouldAllow, ref bool shouldBroadcastOverRadio)
        {
            // Если игрок говорит просто голосом (не в рацию)
            if (!wantsToUseRadio)
            {
                var plugin = RealRadiostationPlugin.Instance;
                var playerPos = __instance.player.transform.position;
                
                // Ищем станцию рядом (используем радиус из конфига)
                var station = plugin.GetNearestStation(playerPos, plugin.Configuration.Instance.TransmitRadius);
                
                if (station != null && station.Mode == RadioMode.TransmitAndListen)
                {
                    // Устанавливаем частоту игроку через quests (как в оригинале)
                    uint freq = (uint)station.Frequency;
                    __instance.player.quests.sendSetRadioFrequency(freq);
                    
                    // Разрешаем трансляцию и включаем эффект рации
                    shouldAllow = true;
                    shouldBroadcastOverRadio = true;
                }
            }
        }
    }
}
