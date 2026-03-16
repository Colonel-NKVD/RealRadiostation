using HarmonyLib;
using SDG.Unturned;
using UnityEngine;

namespace RealRadiostation
{
    public static class VoicePatch
    {
        public static bool Prefix(PlayerVoice __instance, object[] __args)
        {
            // Самый первый дебаг - вошли ли мы вообще в патч?
            // Если этой строки нет в консоли при нажатии микрофона - мы патчим не тот метод.
            Rocket.Core.Logging.Logger.Log($"[VOICE ATTEMPT] Игрок {__instance.player.channel.owner.playerID.characterName} пытается говорить...");

            if (__args == null || __args.Length < 2) return true;

            var player = __instance.player;
            if (player == null || RealRadiostationPlugin.Instance == null) return true;

            float radius = RealRadiostationPlugin.Instance.Configuration.Instance.TransmitRadius;
            // Если в конфиге забыли поставить радиус, ставим 5 метров по умолчанию для теста
            if (radius <= 0) radius = 5f;

            var station = RealRadiostationPlugin.Instance.GetNearestStation(player.transform.position, radius);

            if (station != null)
            {
                Rocket.Core.Logging.Logger.Log($"[VOICE MATCH] Станция найдена рядом с игроком! Режим станции: {station.Mode}");

                if (station.Mode == RadioMode.TransmitAndListen)
                {
                    // Принудительно выставляем частоту
                    player.quests.sendSetRadioFrequency(station.Frequency);
                    
                    // Перезаписываем аргументы метода
                    __args[1] = true; // shouldAllow
                    if (__args.Length > 2) __args[2] = true; // shouldBroadcastOverRadio
                    if (__args.Length > 3) __args[3] = 0f; // SpatialBlend (0 = звук рации везде)

                    Rocket.Core.Logging.Logger.Log($"[VOICE SUCCESS] Голос игрока {player.channel.owner.playerID.characterName} отправлен на волну {station.Frequency}");
                }
            }

            return true;
        }
    }
}
