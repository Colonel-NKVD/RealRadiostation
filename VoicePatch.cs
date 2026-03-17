using HarmonyLib;
using SDG.Unturned;
using UnityEngine;
using System.Reflection;

namespace RealRadiostation
{
    // Мы меняем цель. Вместо askVoiceChat мы берем метод, который отвечает за ретрансляцию.
    // Это самый стабильный метод для работы с голосом в Unturned.
    [HarmonyPatch]
    public static class VoicePatch
    {
        // Динамически находим метод, чтобы Harmony не ругался при загрузке
        [HarmonyTargetMethod]
        static MethodBase TargetMethod()
        {
            return typeof(PlayerVoice).GetMethod("handleVoiceChatRelay", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        [HarmonyPrefix]
        public static void Prefix(PlayerVoice __instance, ref bool wantsToUseRadio, ref bool shouldBroadcastOverRadio)
        {
            // Если игрок уже сам нажал кнопку рации (и она у него есть) — ничего не делаем
            if (wantsToUseRadio) return;

            // Проверяем, есть ли рядом наша стационарная радиостанция
            var station = RealRadiostationPlugin.Instance.GetNearestStation(__instance.player.transform.position, 5f);

            if (station != null && station.Mode == RadioMode.TransmitAndListen)
            {
                // Если частота игрока не совпадает с частотой станции — настраиваем её
                if (__instance.player.quests.radioFrequency != station.Frequency)
                {
                    __instance.player.quests.sendSetRadioFrequency(station.Frequency);
                }

                // КЛЮЧЕВОЙ МОМЕНТ ТЕХНОЛОГИИ:
                // Мы подменяем аргументы метода прямо в полете.
                // Игра думает, что игрок использует рацию (wantsToUseRadio)
                // И мы подтверждаем, что это должно уйти в эфир (shouldBroadcastOverRadio)
                wantsToUseRadio = true;
                shouldBroadcastOverRadio = true;
            }
        }
    }
}
