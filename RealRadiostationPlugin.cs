using Rocket.Core.Plugins;
using SDG.Unturned;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using System.Reflection;

namespace RealRadiostation
{
    public class RealRadiostationPlugin : RocketPlugin<RealRadiostationConfig>
    {
        public static RealRadiostationPlugin Instance;
        public List<RadioStationComponent> ActiveStations = new List<RadioStationComponent>();
        private Harmony harmony;

        protected override void Load()
        {
            Instance = this;

            // Инициализация Harmony с расширенным поиском
            harmony = new Harmony("com.realradiostation.patch");

            var original = typeof(PlayerVoice).GetMethod("receiveRelayVoice", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public) 
                        ?? typeof(PlayerVoice).GetMethod("handleRelayVoiceInternal", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            var prefix = typeof(VoicePatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public);

            if (original != null && prefix != null)
            {
                harmony.Patch(original, new HarmonyMethod(prefix));
                Rocket.Core.Logging.Logger.Log($"[DEBUG] Harmony успешно привязан к методу: {original.Name}");
            }
            else
            {
                Rocket.Core.Logging.Logger.LogError("[DEBUG FATAL] Метод для патча голоса НЕ НАЙДЕН в Assembly-CSharp!");
            }

            StartCoroutine(DeferredStationSearch());
            BarricadeManager.onBarricadeSpawned += OnBarricadeSpawned;
            
            Rocket.Core.Logging.Logger.Log("[DEBUG] Плагин загружен. Мониторинг говорения активен.");
        }

        private IEnumerator DeferredStationSearch()
        {
            Rocket.Core.Logging.Logger.Log("[DEBUG] Ожидание прогрузки баррикад (3 сек)...");
            yield return new WaitForSeconds(3f);
            ScanExistingStations();
        }

        public void ScanExistingStations()
        {
            ActiveStations.Clear();
            var allTransforms = UnityEngine.Object.FindObjectsOfType<Transform>();
            int foundCount = 0;

            foreach (var t in allTransforms)
            {
                var drop = BarricadeManager.FindBarricadeByRootTransform(t);
                if (drop != null && drop.asset.id == Configuration.Instance.RadioBarricadeId)
                {
                    AddStationComponent(drop);
                    foundCount++;
                }
            }
            Rocket.Core.Logging.Logger.Log($"[DEBUG] Сканирование завершено. Найдено радиостанций на карте: {foundCount} (ID в конфиге: {Configuration.Instance.RadioBarricadeId})");
        }

        private void OnBarricadeSpawned(BarricadeRegion region, BarricadeDrop drop)
        {
            if (drop?.asset != null && drop.asset.id == Configuration.Instance.RadioBarricadeId)
            {
                Rocket.Core.Logging.Logger.Log($"[DEBUG] Новая станция установлена игроком. InstanceID: {drop.instanceID}");
                AddStationComponent(drop);
            }
        }

        private void AddStationComponent(BarricadeDrop drop)
        {
            if (drop?.model == null) return;
            var comp = drop.model.gameObject.GetComponent<RadioStationComponent>() ?? drop.model.gameObject.AddComponent<RadioStationComponent>();
            
            var savedData = DataStorage.Load();
            if (savedData != null && savedData.ContainsKey(drop.instanceID))
            {
                comp.Frequency = savedData[drop.instanceID].Frequency;
                comp.Mode = savedData[drop.instanceID].Mode;
            }

            if (!ActiveStations.Contains(comp)) ActiveStations.Add(comp);
            Rocket.Core.Logging.Logger.Log($"[DEBUG] Станция {drop.instanceID} инициализирована. Частота: {comp.Frequency}, Режим: {comp.Mode}");
        }

        public RadioStationComponent GetNearestStation(Vector3 position, float maxDistance)
        {
            RadioStationComponent nearest = null;
            float minSqrDist = maxDistance * maxDistance;

            foreach (var station in ActiveStations)
            {
                if (station == null || station.gameObject == null) continue;
                float sqrDist = (position - station.transform.position).sqrMagnitude;
                if (sqrDist < minSqrDist)
                {
                    minSqrDist = sqrDist;
                    nearest = station;
                }
            }
            return nearest;
        }

        protected override void Unload()
        {
            StopAllCoroutines();
            BarricadeManager.onBarricadeSpawned -= OnBarricadeSpawned;
            if (harmony != null) harmony.UnpatchAll("com.realradiostation.patch");
            ActiveStations.Clear();
            Instance = null;
            Rocket.Core.Logging.Logger.Log("[DEBUG] Плагин выгружен.");
        }
    }
}
