using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using System.Collections.Generic;
using UnityEngine;

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
            RadioStationComponent station = RealRadiostationPlugin.Instance.GetNearestStation(player.Position);

            if (station != null)
            {
                station.ToggleMode();
                RealRadiostationPlugin.Instance.SaveStations();
            }
            else
            {
                UnturnedChat.Say(player, "Рядом не найдено активных радиостанций!", Color.red);
            }
        }
    }
}
