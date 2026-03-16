using HarmonyLib;
using SDG.Unturned;
using UnityEngine;

namespace RealRadiostation
{
    public static class VoicePatch
    {
        public static bool Prefix(PlayerVoice __instance, object[] __args)
        {
            // [0] wantsToUseRadio, [1] shouldAllow, [2] shouldBroadcastOverRadio
            bool wantsToUseRadio = (bool)__args[0];
            string playerName = __instance.player?.channel?.owner?.playerID.characterName ?? "Unknown";

            // Если игрок говорит просто голосом (без рации в руках)
            if (!wantsToUseRadio)
            {
                var plugin = RealRadiostationPlugin.Instance;
                if (plugin == null) return true;

                var pos = __instance.player.transform.position;
                var radius = plugin.Configuration.Instance.TransmitRadius;
                
                var station = plugin.GetNearestStation(pos, radius);
                
                if (station != null)
                {
                    Rocket.Core.Logging.Logger.Log($"DEBUG VOICE: Игрок {playerName} рядом со станцией (Частота: {station.Frequency}, Режим: {station.Mode})");

                    if (station.Mode == RadioMode.TransmitAndListen)
                    {
                        uint freq = (uint)station.Frequency;
                        __instance.player.quests.sendSetRadioFrequency(freq);
                        
                        __args[1] = true; // shouldAllow
                        __args[2] = true; // shouldBroadcastOverRadio
                        
                        Rocket.Core.Logging.Logger.Log($"DEBUG VOICE: Голос игрока {playerName} ПЕРЕНАПРАВЛЕН на частоту {freq}");
                    }
                    else 
                    {
                        Rocket.Core.Logging.Logger.Log($"DEBUG VOICE: Станция найдена, но режим '{station.Mode}' не позволяет передачу.");
                    }
                }
            }
            else
            {
                // Если игрок УЖЕ говорит в рацию (сам нажал кнопку)
                // Rocket.Core.Logging.Logger.Log($"DEBUG VOICE: Игрок {playerName} говорит в свою рацию.");
            }
            return true;
        }
    }
}
