using HarmonyLib;
using SDG.Unturned;
using UnityEngine;

namespace RealRadiostation
{
    public static class VoicePatch
    {
        public static bool Prefix(PlayerVoice __instance, object[] __args)
        {
            // Нам нужно как минимум 3 аргумента (wantsToUseRadio, shouldAllow, shouldBroadcast)
            if (__args == null || __args.Length < 3) return true;
            
            bool wantsToUseRadio = false;
            if (__args[0] is bool b) wantsToUseRadio = b;

            // Если игрок уже нажал кнопку рации в руках — не трогаем, пусть работает стандартно
            if (wantsToUseRadio) return true;

            var player = __instance.player;
            if (player == null || RealRadiostationPlugin.Instance == null) return true;

            // Проверяем дистанцию до ближайшей станции 1466
            float radius = RealRadiostationPlugin.Instance.Configuration.Instance.TransmitRadius;
            var station = RealRadiostationPlugin.Instance.GetNearestStation(player.transform.position, radius);

            if (station != null && station.Mode == RadioMode.TransmitAndListen)
            {
                // ЛОГИКА РЕТРАНСЛЯЦИИ:
                // 1. Принудительно выставляем частоту игрока на частоту станции
                player.quests.sendSetRadioFrequency(station.Frequency);
                
                // 2. Включаем передачу в эфир, даже если у игрока нет рации
                __args[1] = true; // shouldAllow
                __args[2] = true; // shouldBroadcastOverRadio
                
                // 3. Убираем пространственный звук (делаем голос четким)
                if (__args.Length > 3) __args[3] = 0f;

                Rocket.Core.Logging.Logger.Log($"[VOICE] Игрок {player.channel.owner.playerID.characterName} вещает через станцию (Частота: {station.Frequency})");
            }

            return true;
        }
    }
}
