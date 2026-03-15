using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using System.Collections.Generic;
using UnityEngine;
using SDG.Unturned;
using Steamworks;

namespace RealRadiostation
{
    public class CommandRadioChange : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "radiochange";
        public string Help => "Переключает режим ближайшей радиостанции";
        public string Syntax => "";
        public List<string> Aliases => new List<string> { "rch" };
        public List<string> Permissions => new List<string> { "realradio.admin" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer player = (UnturnedPlayer)caller;
            
            if (RealRadiostationPlugin.Instance == null)
            {
                UnturnedChat.Say(player, "[Debug] Ошибка: Экземпляр плагина не найден!", Color.red);
                return;
            }

            // Ищем ближайшую станцию
            RadioStationComponent station = RealRadiostationPlugin.Instance.GetNearestStation(player.Position);

            if (station != null)
            {
                // Переключаем режим
                station.ToggleMode();
                
                // Сохраняем данные
                RealRadiostationPlugin.Instance.SaveStations();

                // Формируем статус для лога и чата
                string modeStatus = (station.Mode == RadioMode.ListenOnly) ? "Только прослушивание" : "Прием и передача";
                
                // Отправляем системное сообщение игроку
                UnturnedChat.Say(player, $"[Radio] Режим успешно изменен на: {modeStatus}", Color.green);
                
                // Дублируем в локальный чат (как при смене волны), чтобы видели окружающие
                ChatManager.say(CSteamID.Nil, $"Станция рядом переключена в режим: {modeStatus}", Color.yellow, EChatMode.LOCAL);
            }
            else
            {
                // Сообщение, если игрок стоит слишком далеко или ID в конфиге не совпадает
                UnturnedChat.Say(player, "[Debug] Станция не найдена! Проверьте ID в конфиге и подойдите ближе (3м).", Color.red);
            }
        }
    }
}
