using HarmonyLib;
using SDG.Unturned;
using UnityEngine;
using System.Reflection;

namespace RealRadiostation
{
    public static class VoicePatch
    {
        public static bool Prefix(PlayerVoice __instance, MethodBase __originalMethod, object[] __args)
        {
            // ЛОГ №1: Мы вообще зашли в какой-либо метод?
            Rocket.Core.Logging.Logger.Log($"[DETECTOR] Вызван метод: {__originalMethod.Name} от игрока {__instance.player.channel.owner.playerID.characterName}");

            var player = __instance.player;
            if (RealRadiostationPlugin.Instance == null) return true;

            // Проверка дистанции (увеличим для теста до 10 метров)
            float radius = 10f; 
            var station = RealRadiostationPlugin.Instance.GetNearestStation(player.transform.position, radius);

            if (station != null)
            {
                Rocket.Core.Logging.Logger.Log($"[DETECTOR] Игрок РЯДОМ со станцией {station.Frequency}");

                // Пытаемся принудительно включить рацию в аргументах
                if (__args != null)
                {
                    for (int i = 0; i < __args.Length; i++)
                    {
                        if (__args[i] is bool)
                        {
                            __args[i] = true; // Подменяем обычный голос на радио
                            Rocket.Core.Logging.Logger.Log($"[DETECTOR] Аргумент {i} изменен на TRUE (Радио)");
                        }
                    }
                }

                // Силовое переключение частоты
                player.quests.sendSetRadioFrequency(station.Frequency);
            }
            else
            {
                // Если станция не найдена, пишем дистанцию до первой попавшейся для дебага
                if (RealRadiostationPlugin.Instance.ActiveStations.Count > 0)
                {
                    float d = Vector3.Distance(player.transform.position, RealRadiostationPlugin.Instance.ActiveStations[0].transform.position);
                    Rocket.Core.Logging.Logger.Log($"[DETECTOR] Станция далеко. Дистанция: {d:F1}м (нужно < {radius}м)");
                }
            }

            return true;
        }
    }
}
