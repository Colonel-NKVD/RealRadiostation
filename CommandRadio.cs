using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using System.Collections.Generic;
using UnityEngine;
using SDG.Unturned;
using System.Globalization;

namespace RealRadiostation
{
    public class CommandRadio : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "radio";
        public string Help => "Устанавливает частоту ближайшей радиостанции";
        public string Syntax => "<frequency>";
        public List<string> Aliases => new List<string> { "freq" };
        public List<string> Permissions => new List<string> { "realradio.admin" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer player = (UnturnedPlayer)caller;

            // Используем float для поддержки дробных частот и обрабатываем разделители (точка/запятая)
            if (command.Length != 1 || !float.TryParse(command[0].Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out float newFreq))
            {
                UnturnedChat.Say(player, "Использование: /radio <частота> (например, 111.111)", Color.red);
                return;
            }

            // Ищем ближайшую радиостанцию
            RadioStationComponent nearestStation = RealRadiostationPlugin.Instance.GetNearestStation(player.Position);

            if (nearestStation != null)
            {
                nearestStation.Frequency = newFreq;
                // Сохраняем изменения в файл StationsData.json
                RealRadiostationPlugin.Instance.SaveStations(); 
                
                UnturnedChat.Say(player, $"Частота радиостанции установлена на: {newFreq:F3} MHz", Color.cyan);
            }
            else
            {
                UnturnedChat.Say(player, "Рядом не найдено активных радиостанций!", Color.red);
            }
        }
    }
}
