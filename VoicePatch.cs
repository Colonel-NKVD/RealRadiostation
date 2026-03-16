using HarmonyLib;
using SDG.Unturned;
using UnityEngine;

namespace RealRadiostation
{
    public static class VoicePatch
    {
        public static bool Prefix(PlayerVoice __instance, object[] __args)
        {
            // [0] wantsToUseRadio (bool) - нажал ли игрок кнопку рации
            // [1] shouldAllow (ref bool) - разрешить ли передачу
            // [2] shouldBroadcastOverRadio (ref bool) - транслировать ли на частоте
            
            bool wantsToUseRadio = (bool)__args[0];
            var player = __instance.player;
            if (player == null) return true;

            // Если игрок УЖЕ говорит через Walkie-Talkie в руках, мы не мешаем игре.
            // Игра сама отправит голос на частоту рации в его руках.
            if (wantsToUseRadio) return true; 

            var plugin = RealRadiostationPlugin.Instance;
            if (plugin == null) return true;

            float radius = plugin.Configuration.Instance.TransmitRadius;
            var station = plugin.GetNearestStation(player.transform.position, radius);

            if (station != null)
            {
                string pName = player.channel.owner.playerID.characterName;

                if (station.Mode == RadioMode.TransmitAndListen)
                {
                    // ПРОФЕССИОНАЛЬНАЯ РЕТРАНСЛЯЦИЯ
                    // 1. Принудительно ставим игроку частоту станции
                    player.quests.sendSetRadioFrequency(station.Frequency);
                    
                    // 2. Говорим игре, что этот голос (из обычного чата) должен уйти в эфир
                    __args[1] = true; // shouldAllow = true
                    __args[2] = true; // shouldBroadcastOverRadio = true
                    
                    // 3. Если в игре есть параметр SpatialBlend (4-й в списке), убираем его в 0 (чистый звук рации)
                    if (__args.Length > 3) __args[3] = 0f;

                    Rocket.Core.Logging.Logger.Log($"[VOICE DEBUG] Игрок {pName} заговорил у станции. Частота {station.Frequency} ПРИМЕНЕНА.");
                }
                else 
                {
                    Rocket.Core.Logging.Logger.Log($"[VOICE DEBUG] Игрок {pName} у станции, но она в режиме 'Только прием'. Перехват отменен.");
                }
            }

            return true;
        }
    }
}
