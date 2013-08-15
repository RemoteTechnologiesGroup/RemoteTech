using System;
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
            dump.Append('-', 80).AppendLine();

            dump.AppendLine("Active VesselSatellites: ").Append('-', 80).AppendLine();
            foreach (var satellite in RTCore.Instance.Satellites) {
                dump.AppendFormat("Name: {0} ({1})", satellite.Name, satellite.Guid).AppendLine();
                dump.AppendFormat("Powered: {0}", satellite.Powered.ToString()).AppendLine();
                dump.AppendFormat("Connection: {0}", satellite.Connection.Exists.ToString()).AppendLine();
                dump.AppendFormat("OmniRange: {0}", satellite.Omni.ToString()).AppendLine();
                foreach(var dish in satellite.Dishes) {
                    var target = RTCore.Instance.Network[dish.Target];
                    dump.AppendFormat("AbstractDish(Target = {0} ({1}), Distance = {2}, Factor = {3})",
                                      target == null ? "Unknown Target" : target.Name, dish.Target, dish.Distance, dish.Factor).AppendLine();
                }
                foreach(var antenna in RTCore.Instance.Antennas.For(satellite.Vessel)) {
                    dump.AppendFormat("PhysicalAntenna(Name = {0}, Powered = {1}, Omni = {2}, Dish = {3}, Target = {4}, Proto = {5})",
                        antenna.Name, "N/A", antenna.OmniRange, antenna.DishRange, antenna.DishTarget, antenna is ProtoAntenna).AppendLine();
                }
                dump.AppendLine();
            }
            dump.AppendLine();

            dump.AppendLine("Active Antennas").Append('-', 80).AppendLine();
            foreach (var antenna in RTCore.Instance.Antennas) {
                dump.AppendFormat("Antenna(Name = {0}, Powered = {1}, Omni = {2}, Dish = {3}, Target = {4}",
                                  antenna.Name, "N/A", antenna.OmniRange, antenna.DishRange, antenna.DishTarget).AppendLine();
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