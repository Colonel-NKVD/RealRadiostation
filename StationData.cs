using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace RealRadiostation
{
    public class StationData
    {
        public uint Frequency { get; set; }
        public bool CanTransmit { get; set; }
    }

    public static class DataStorage
    {
        private static string FilePath => Path.Combine(RealRadiostationPlugin.Instance.Directory, "StationsData.json");

        public static Dictionary<string, StationData> Load()
        {
            if (!File.Exists(FilePath)) return new Dictionary<string, StationData>();
            string json = File.ReadAllText(FilePath);
            return JsonConvert.DeserializeObject<Dictionary<string, StationData>>(json) ?? new Dictionary<string, StationData>();
        }

        public static void Save(Dictionary<string, StationData> data)
        {
            File.WriteAllText(FilePath, JsonConvert.SerializeObject(data, Formatting.Indented));
        }
    }
}
