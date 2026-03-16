using HarmonyLib;
using SDG.Unturned;
using UnityEngine;

namespace RealRadiostation
{
    public static class VoicePatch
    {
        public static bool Prefix(PlayerVoice __instance, object[] __args)
        {
            // Пытаемся безопасно извлечь 'wantsToUseRadio' (обычно это 1-й аргумент)
            if (__args.Length < 1) return true;
            
            bool wantsToUseRadio = false;
            if (__args[0] is bool b) wantsToUseRadio = b;

            // Если игрок УЖЕ использует рацию в руках, не вмешиваемся
            if (wantsToUseRadio) return true;

            var player = __instance.player;
            if (player == null || RealRadiostationPlugin.Instance == null) return true;

            float radius = RealRadiostationPlugin.Instance.Configuration.Instance.TransmitRadius;
            var station = RealRadiostationPlugin.Instance.GetNearestStation(player.transform.position, radius);

            if (station != null && station.Mode == RadioMode.TransmitAndListen)
            {
                // ГАРАНТИЯ 2: Ретрансляция голоса на частоту станции
                // Синхронизируем частоту игрока с частотой стационарной рации
                player.quests.sendSetRadioFrequency(station.Frequency);

                // Подменяем аргументы вызова метода игры на лету
                if (__args.Length >= 3)
                {
                    __args[1] = true; // shouldAllow
                    __args[2] = true; // shouldBroadcastOverRadio
                }
                
                // Убираем затухание звука (делаем звук как в рации)
                if (__args.Length > 3) __args[3] = 0f;

                Rocket.Core.Logging.Logger.Log($"[VOICE] Голос игрока {player.channel.owner.playerID.characterName} перенаправлен на волну {station.Frequency}");
            }

            return true;
        }
    }
}
