using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using System.Collections.Generic;

namespace RealRadiostation
{
    public class CommandRadioFreq : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "radiofreq";
        public string Help => "Установить частоту ближайшей стационарной рации";
        public string Syntax => "/radiofreq <частота>";
        public List<string> Aliases => new List<string> { "rfreq" };
        public List<string> Permissions => new List<string> { "radiofreq" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer player = (UnturnedPlayer)caller;

            if (command.Length != 1 || !uint.TryParse(command[0], out uint freq))
            {
                UnturnedChat.Say(caller, "Использование: /radiofreq <частота>", UnityEngine.Color.red);
                return;
            }

            var station = RealRadiostationPlugin.Instance.GetNearestStation(player.Position, RealRadiostationPlugin.Instance.Configuration.Instance.SetupCommandRadius, false);
            
            if (station == null)
            {
                UnturnedChat.Say(caller, "Рядом нет радиостанции для настройки.", UnityEngine.Color.yellow);
                return;
            }

            station.Frequency = freq;
            RealRadiostationPlugin.Instance.SaveAllStations();
            UnturnedChat.Say(caller, $"[ЭФИР] Частота станции установлена на {freq}.", UnityEngine.Color.green);
        }
    }

    public class CommandRadioMode : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "radiomode";
        public string Help => "Переключить режим станции (слушать / говорить)";
        public string Syntax => "/radiomode";
        public List<string> Aliases => new List<string> { "rmode" };
        public List<string> Permissions => new List<string> { "radiomode" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer player = (UnturnedPlayer)caller;

            var station = RealRadiostationPlugin.Instance.GetNearestStation(player.Position, RealRadiostationPlugin.Instance.Configuration.Instance.SetupCommandRadius, false);

            if (station == null)
            {
                UnturnedChat.Say(caller, "Рядом нет радиостанции для настройки.", UnityEngine.Color.yellow);
                return;
            }

            station.CanTransmit = !station.CanTransmit;
            RealRadiostationPlugin.Instance.SaveAllStations();

            string mode = station.CanTransmit ? "Слушать и Говорить" : "Только слушать";
            UnturnedChat.Say(caller, $"[ЭФИР] Режим станции изменен на: {mode}.", UnityEngine.Color.cyan);
        }
    }
}
