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
            // ЛОГ №1: Фиксируем любое использование микрофона
            Rocket.Core.Logging.Logger.Log($"[DETECTOR] Вызван метод: {__originalMethod.Name} от игрока {__instance.player.channel.owner.playerID.characterName}");

            var player = __instance.player;
            if (RealRadiostationPlugin.Instance == null) return true;

            // Тестовый радиус 10 метров
            float radius = 10f; 
            var station = RealRadiostationPlugin.Instance.GetNearestStation(player.transform.position, radius);

            if (station != null)
            {
                Rocket.Core.Logging.Logger.Log($"[DETECTOR] Игрок РЯДОМ со станцией {station.Frequency}");

                if (__args != null)
                {
                    for (int i = 0; i < __args.Length; i++)
                    {
                        if (__args[i] is bool)
                        {
                            __args[i] = true; 
                            Rocket.Core.Logging.Logger.Log($"[DETECTOR] Аргумент {i} (bool) изменен на TRUE");
                        }
                    }
                }

                player.quests.sendSetRadioFrequency(station.Frequency);
            }
            else
            {
                // Если станций нет рядом, проверим, есть ли они вообще в мире
                if (RealRadiostationPlugin.Instance.ActiveStations.Count > 0)
                {
                    float d = Vector3.Distance(player.transform.position, RealRadiostationPlugin.Instance.ActiveStations[0].transform.position);
                    Rocket.Core.Logging.Logger.Log($"[DETECTOR] Станция 1466 найдена, но она далеко: {d:F1}м");
                }
                else
                {
                    Rocket.Core.Logging.Logger.Log("[DETECTOR] Станций с ID 1466 в активном списке нет.");
                }
            }

            return true;
        }
    }
}
