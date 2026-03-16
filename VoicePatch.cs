using HarmonyLib;
using SDG.Unturned;
using UnityEngine;

namespace RealRadiostation
{
    public static class VoicePatch
    {
        public static bool Prefix(PlayerVoice __instance, object[] __args)
        {
            var player = __instance.player;
            if (player == null || RealRadiostationPlugin.Instance == null) return true;

            // Берем радиус из конфига. Если там 0, ставим 5 метров.
            float radius = RealRadiostationPlugin.Instance.Configuration.Instance.TransmitRadius;
            if (radius <= 0) radius = 5f;

            var station = RealRadiostationPlugin.Instance.GetNearestStation(player.transform.position, radius);

            if (station != null && station.Mode == RadioMode.TransmitAndListen)
            {
                // 1. Используем официальный метод для смены частоты (этот метод НЕ read-only)
                player.quests.sendSetRadioFrequency(station.Frequency);

                bool argModified = false;

                // 2. Ищем параметр wantsToUseRadio в аргументах метода askVoiceChat
                // и принудительно заставляем игру думать, что игрок использует рацию
                if (__args != null)
                {
                    for (int i = 0; i < __args.Length; i++)
                    {
                        if (__args[i] is bool)
                        {
                            __args[i] = true; // Включаем режим трансляции на всю карту
                            argModified = true;
                            break; 
                        }
                    }
                }

                Rocket.Core.Logging.Logger.Log($"[VOICE SUCCESS] Игрок {player.channel.owner.playerID.characterName} вещает в эфир! Волна: {station.Frequency} | Перехват: {argModified}");
            }

            return true; 
        }
    }
}
