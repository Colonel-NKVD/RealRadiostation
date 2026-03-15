using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using System.Collections.Generic;
using UnityEngine;
using SDG.Unturned;

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

            if (command.Length != 1 || !uint.TryParse(command[0], out uint newFreq))
            {
                UnturnedChat.Say(player, "Использование: /radio <частота>", Color.red);
                return;
            }

            // Ищем ближайшую радиостанцию в небольшом радиусе
            RadioStationComponent nearestStation = null;
            float minDistance = 3f; 

            foreach (var station in RealRadiostationPlugin.Instance.ActiveStations)
            {
                float dist = Vector3.Distance(player.Position, station.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearestStation = station;
                }
            }

            if (nearestStation != null)
            {
                nearestStation.Frequency = newFreq;
                // Адаптация: сохраняем изменения в файл после установки частоты
                RealRadiostationPlugin.Instance.SaveStations(); 
                
                UnturnedChat.Say(player, $"Частота радиостанции установлена на: {newFreq} MHz", Color.cyan);
            }
            else
            {
                UnturnedChat.Say(player, "Рядом не найдено активных радиостанций!", Color.red);
            }
        }
    }
}
