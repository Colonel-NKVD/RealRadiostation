using HarmonyLib;
using SDG.Unturned;
using UnityEngine;
using System.Reflection;

namespace RealRadiostation
{
    public static class VoicePatch
    {
        public static void UniversalPrefix(object __instance, MethodBase __originalMethod)
        {
            // Мы ловим ВООБЩЕ ВСЁ, кроме Update.
            // Если и это не сработает - значит голос идет через нативные методы Steam, которые Harmony не видит.
            
            string name = __originalMethod.Name;
            
            // Фильтруем только то, что похоже на передачу данных или голос
            if (name.Contains("Voice") || name.Contains("Chat") || name.Contains("Relay") || name.Contains("Send") || name.Contains("Ask"))
            {
                string playerName = "Unknown";
                if (__instance is PlayerVoice pv) playerName = pv.player.channel.owner.playerID.characterName;
                if (__instance is PlayerQuests pq) playerName = pq.player.channel.owner.playerID.characterName;

                Rocket.Core.Logging.Logger.Log($"[GLOBAL HIT] Метод: {name} | Класс: {__originalMethod.DeclaringType.Name} | Игрок: {playerName}");
            }
        }
    }
}
