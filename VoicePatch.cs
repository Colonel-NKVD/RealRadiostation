using HarmonyLib;
using SDG.Unturned;
using UnityEngine;
using System.Reflection;

namespace RealRadiostation
{
    public static class VoicePatch
    {
        public static void UniversalPrefix(PlayerVoice __instance, MethodBase __originalMethod, object[] __args)
        {
            string methodName = __originalMethod.Name;

            // Игнорируем методы-спамеры, чтобы не вешать сервер
            if (methodName == "Update" || methodName == "FixedUpdate" || methodName.StartsWith("get_")) 
                return;

            // Логируем только важные события
            Rocket.Core.Logging.Logger.Log($"[VOICE DETECTED] Вызван метод: {methodName} | Игрок: {__instance.player.channel.owner.playerID.characterName}");

            // Проверяем, есть ли рядом станция
            var station = RealRadiostationPlugin.Instance.GetNearestStation(__instance.player.transform.position, 10f);
            if (station != null && station.Mode == RadioMode.TransmitAndListen)
            {
                // Если мы нашли метод, который срабатывает при нажатии кнопки (например askVoiceChat)
                // то пытаемся подменить параметры
                if (__args != null)
                {
                    for (int i = 0; i < __args.Length; i++)
                    {
                        if (__args[i] is bool)
                        {
                            __args[i] = true; // Пытаемся заставить игру использовать радио
                            Rocket.Core.Logging.Logger.Log($"[ACTION] Аргумент {i} в методе {methodName} изменен на TRUE (Радио режим)");
                        }
                    }
                }
                
                // Силовая установка частоты
                __instance.player.quests.sendSetRadioFrequency(station.Frequency);
            }
        }
    }
}
