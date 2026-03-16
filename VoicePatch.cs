using HarmonyLib;
using SDG.Unturned;
using UnityEngine;
using System.Reflection;

namespace RealRadiostation
{
    public static class VoicePatch
    {
        // Специальный префикс, который принимает информацию о ЛЮБОМ методе
        public static void UniversalPrefix(PlayerVoice __instance, MethodBase __originalMethod)
        {
            // Это сообщение ДОЛЖНО появиться, если хоть какой-то метод PlayerVoice сработал
            Rocket.Core.Logging.Logger.Log($"[HIT] Сработал метод: {__originalMethod.Name} | Игрок: {__instance.player.channel.owner.playerID.characterName}");
            
            // Если это один из ключевых методов, пробуем подменить частоту
            if (__originalMethod.Name.ToLower().Contains("voice") || __originalMethod.Name.ToLower().Contains("relay"))
            {
                var station = RealRadiostationPlugin.Instance.GetNearestStation(__instance.player.transform.position, 10f);
                if (station != null)
                {
                    __instance.player.quests.sendSetRadioFrequency(station.Frequency);
                    Rocket.Core.Logging.Logger.Log($"[ACTION] Попытка форсировать частоту {station.Frequency}");
                }
            }
        }
    }
}
