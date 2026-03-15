using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace RealRadiostation
{
    public class StationData
    {
        public float Frequency { get; set; }
        public RadioMode Mode { get; set; }
    }

    public static class DataStorage
    {
        private static string Path => "Plugins/RealRadiostation/StationsData.json";

        public static void Save(Dictionary<ulong, StationData> data)
        {
            File.WriteAllText(Path, JsonConvert.SerializeObject(data, Formatting.Indented));
        }

        public static Dictionary<ulong, StationData> Load()
        {
            if (!File.Exists(Path)) return new Dictionary<ulong, StationData>();
            return JsonConvert.DeserializeObject<Dictionary<ulong, StationData>>(File.ReadAllText(Path));
        }
    }
}
