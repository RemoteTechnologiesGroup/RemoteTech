using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace RemoteTech {
    public class DebugUnit : IDisposable {
        private readonly RTCore mCore;
        
        public DebugUnit(RTCore core) {
            mCore = core;
            mCore.GuiUpdated += OnKey;
        }

        public void Dispose() {
            mCore.GuiUpdated -= OnKey;
        }

        private void OnKey() {
            if (Event.current.isKey && Event.current.keyCode == KeyCode.F11) {
                Dump();
            }
        }

        private void Dump() {
            var dump = new StringBuilder();
            dump.AppendFormat("RemoteTech State @ {0}, ID: {1}",
                              System.DateTime.Now.ToUniversalTime(),
                              GitRevisionProvider.GetHash()).AppendLine();
            dump.Append('-', 80).AppendLine().AppendLine();
            
            foreach (var satellite in RTCore.Instance.Satellites) {
                dump.AppendLine().AppendLine("VesselSatellite:").Append('-', 80).AppendLine();
                var satellite_dict = new OrderedDictionary();
                satellite_dict.Add("Name", satellite.Name);
                satellite_dict.Add("Guid", satellite.Guid.ToString());
                satellite_dict.Add("AbstractOmniRange", satellite.Omni.ToString());

                var abstract_dishes = new StringBuilder().AppendLine();
                foreach (var dish in satellite.Dishes) {
                    var dish_dict = new OrderedDictionary();
                    dish_dict.Add("Target", dish.Target);
                    dish_dict.Add("Target Name", RTCore.Instance.Network[dish.Target] == null ? "Unknown Target" : RTCore.Instance.Network[dish.Target].Name);
                    dish_dict.Add("Distance", dish.Distance);
                    dish_dict.Add("Dish Factor", dish.Factor);
                    dish_dict.Add("Dish Angle", (Math.Acos(dish.Factor) / Math.PI * 180 * 2).ToString("F2") + " degrees");
                    foreach (DictionaryEntry kvp in dish_dict) {
                        abstract_dishes.AppendFormat("    {0}: {1}", kvp.Key.ToString(), kvp.Value.ToString()).AppendLine();
                    }
                    abstract_dishes.AppendLine();
                }

                var physical_antennas = new StringBuilder().AppendLine();
                foreach(var antenna in RTCore.Instance.Antennas[satellite]) {
                    var antenna_dict = new OrderedDictionary();
                    antenna_dict.Add("Name", antenna.Name);
                    antenna_dict.Add("Proto?", antenna is ProtoAntenna);
                    antenna_dict.Add("Powered", "N/A");
                    antenna_dict.Add("OmniRange", antenna.CurrentOmniRange);
                    antenna_dict.Add("DishRange", antenna.CurrentDishRange);
                    antenna_dict.Add("Target Guid", antenna.DishTarget);
                    foreach (DictionaryEntry kvp in antenna_dict) {
                        physical_antennas.AppendFormat("    {0}: {1}", kvp.Key.ToString(), kvp.Value.ToString()).AppendLine();
                    }
                    physical_antennas.AppendLine();
                }

                satellite_dict.Add("AbstractDishes", abstract_dishes);
                satellite_dict.Add("PhysicalAntennas", physical_antennas);

                foreach (DictionaryEntry kvp in satellite_dict) {
                    dump.AppendFormat("{0}: {1}", kvp.Key.ToString(), kvp.Value.ToString()).AppendLine();
                }
            }
            dump.AppendLine();

            dump.AppendLine("All Antennas").Append('-', 80).AppendLine();
            foreach (var antenna in RTCore.Instance.Antennas) {
                var antenna_dict = new OrderedDictionary();
                antenna_dict.Add("Name", antenna.Name);
                antenna_dict.Add("Proto?", antenna is ProtoAntenna);
                antenna_dict.Add("Powered", "N/A");
                antenna_dict.Add("OmniRange", antenna.CurrentOmniRange);
                antenna_dict.Add("DishRange", antenna.CurrentDishRange);
                antenna_dict.Add("Target Guid", antenna.DishTarget);
                foreach (DictionaryEntry kvp in antenna_dict) {
                    dump.AppendFormat("{0}: {1}", kvp.Key.ToString(), kvp.Value.ToString()).AppendLine();
                }
                dump.AppendLine();
            }

            System.IO.File.WriteAllText(@"./rt_dump.txt", dump.ToString());
        }


    }

    internal static class GitRevisionProvider {
        public static string GetHash() {
            using (var stream = Assembly.GetExecutingAssembly()
                                        .GetManifestResourceStream("RemoteTech" + "." + "version.txt"))
            using (var reader = new StreamReader(stream)) {
                return reader.ReadLine();
            }
        }
    }
}