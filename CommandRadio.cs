using Rocket.API;
using Rocket.Unturned.Chat;
using UnityEngine;
using System.Collections.Generic;

namespace RealRadiostation
{
    public class CommandRadioFreq : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "radiofreq";
        public string Help => "Сменить волну ближайшей радиостанции";
        public string Syntax => "/radiofreq <волна>";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "realradiostation.radiofreq" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            if (command.Length < 1)
            {
                UnturnedChat.Say(caller, "Используйте: /radiofreq <число>", Color.red);
                return;
            }

            var player = (Rocket.Unturned.Player.UnturnedPlayer)caller;
            var plugin = RealRadiostationPlugin.Instance;
            var station = plugin.GetNearestStation(player.Position, plugin.Configuration.Instance.TransmitRadius);

            if (station == null)
            {
                UnturnedChat.Say(caller, "Вы не находитесь рядом с радиостанцией!", Color.red);
                return;
            }

            // Умный парсер частоты (убирает точки и запятые)
            string rawInput = command[0].Replace(".", "").Replace(",", "");
            
            if (uint.TryParse(rawInput, out uint newFreq))
            {
                station.Frequency = newFreq;
                plugin.SaveAllStations();
                UnturnedChat.Say(caller, $"Волна станции успешно изменена на {newFreq}", Color.green);
            }
            else
            {
                UnturnedChat.Say(caller, "Ошибка: Волна должна быть числом (например, 111.111)", Color.red);
            }
        }
    }

    public class CommandRadioMode : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "radiomode";
        public string Help => "Переключить режим радиостанции";
        public string Syntax => "/radiomode";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "realradiostation.radiomode" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            var player = (Rocket.Unturned.Player.UnturnedPlayer)caller;
            var plugin = RealRadiostationPlugin.Instance;
            var station = plugin.GetNearestStation(player.Position, plugin.Configuration.Instance.TransmitRadius);

            if (station == null)
            {
                UnturnedChat.Say(caller, "Вы не находитесь рядом с радиостанцией!", Color.red);
                return;
            }

            station.Mode = station.Mode == RadioMode.ListenOnly ? RadioMode.TransmitAndListen : RadioMode.ListenOnly;
            plugin.SaveAllStations();
            
            string modeText = station.Mode == RadioMode.TransmitAndListen ? "Прием и Передача" : "Только Прием";
            UnturnedChat.Say(caller, $"Режим станции изменен на: {modeText}", Color.green);
        }
    }
}
