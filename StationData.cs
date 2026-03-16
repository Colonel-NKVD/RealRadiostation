using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RealRadiostation
{
    public enum RadioMode { ListenOnly, TransmitAndListen }

    public class StationData
    {
        public uint Frequency { get; set; }
        public RadioMode Mode { get; set; }
    }

    public class RadioStationComponent : MonoBehaviour
    {
        public uint Frequency;
        public RadioMode Mode;
    }

    public static class DataStorage
    {
        private static string Path => "Plugins/RealRadiostation/StationsData.json";

        public static void Save(Dictionary<string, StationData> data)
        {
            string dir = System.IO.Path.GetDirectoryName(Path);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(Path, JsonConvert.SerializeObject(data, Formatting.Indented));
        }

        public static Dictionary<string, StationData> Load()
        {
            if (!File.Exists(Path)) return new Dictionary<string, StationData>();
            try {
                return JsonConvert.DeserializeObject<Dictionary<string, StationData>>(File.ReadAllText(Path));
            } catch {
                return new Dictionary<string, StationData>();
            }
        }
    }
}
