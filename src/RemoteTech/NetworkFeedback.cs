using System;
using System.Collections.Generic;

namespace RemoteTech
{
    public class NetworkFeedback
    {
        /// <summary>Returns the position of a target identified only by its Guid</summary>
        /// <returns>An absolute world coordinate position.</returns>
        /// <param name="target">The item whose position is desired. May be either a satellite 
        /// or a celestial body</param>
        /// Throws System.ArgumentException if target or network does not exist.
        public static Vector3d targetPosition(Guid target) {
            if (RTCore.Instance != null && RTCore.Instance.Network != null &&
                target != Guid.Empty) {
                ISatellite targetSat = RTCore.Instance.Network[target];
                if (targetSat != null) {
                    return targetSat.Position;
                }

                try {
                    CelestialBody targetPlanet = RTCore.Instance.Network.Planets[target];
                    return targetPlanet.position;
                } catch (KeyNotFoundException) {}
            }

            throw new System.ArgumentException("No such Guid found", "target");
        }

        /// <summary>Returns the SoI of a target identified only by its Guid</summary>
        /// <returns>If target refers to a celestial body, returns that body. If target refers 
        /// to an ISatellite, returns the body whose SoI currently contains the ISatellite.</returns>
        /// <param name="target">The item whose position is desired. May be either a satellite 
        /// or a celestial body.</param>
        /// Throws System.ArgumentException if target or network does not exist.
        public static CelestialBody targetBody(Guid target) {
            if (RTCore.Instance != null && RTCore.Instance.Network != null &&
                target != Guid.Empty) {
                ISatellite targetSat = RTCore.Instance.Network[target];
                if (targetSat != null) {
                    return targetSat.Body;
                }

                try {
                    CelestialBody targetPlanet = RTCore.Instance.Network.Planets[target];
                    return targetPlanet;
                } catch (KeyNotFoundException) {}
            }

            throw new System.ArgumentException("No such Guid found", "target");
        }

        /// <summary>Counts the number of ISatellites that are in the antenna cone and in range.</summary>
        /// <returns>The number of reachable satellites.</returns>
        /// <remarks>If antenna does not have a cone, returns 0.</remarks>
        /// <param name="antenna">The antenna whose cone must be tested.</param>
        /// <param name="target">The object the cone is pointed at.</param>
        public static int countInCone(IAntenna antenna, Guid target) {
            if (antenna.Dish <= 0.0 || antenna.CosAngle >= 1.0) {
                return 0;
            }

            try {
                Vector3d myPos = RTCore.Instance.Network[antenna.Guid].Position;
                Vector3d targetDir = (targetPosition(target) - myPos).normalized;
                CelestialBody myBody = targetBody(target);

                int count = 0;
                foreach (ISatellite sat in RTCore.Instance.Satellites) {
                    Vector3d satDir = (sat.Position - myPos).normalized;
                    if (Vector3d.Distance(myPos, sat.Position) <= antenna.Dish  // in range
                        && Vector3d.Dot(targetDir, satDir) >= antenna.CosAngle  // in cone
                        && sat.Body == myBody)                                  // in target SoI... remove later

                        count++;
                }

                return count;
            } catch (ArgumentException) {
                return 0;
            } catch (NullReferenceException) {
                return 0;
            }
        }

        /// <summary>Tests whether an antenna can connect to a target</summary>
        /// <returns>The range to the target, or a diagnostic error message. Returns the 
        /// empty string if target is invalid.</returns>
        /// <param name="antenna">The antenna attempting to make a connection.</param>
        /// <param name="target">The Guid to which it is trying to connect.</param>
        public static KeyValuePair<string, UnityEngine.Color> tryConnection(IAntenna antenna, Guid target) {
            // What kind of target?
            if (RTCore.Instance != null && RTCore.Instance.Network != null &&
                    target != Guid.Empty && target != NetworkManager.ActiveVesselGuid) {
                bool warning = false, error = false;

                ISatellite mySat = RTCore.Instance.Network[antenna.Guid];
                if (mySat == null)
                    return new KeyValuePair<string, UnityEngine.Color>("", UnityEngine.Color.white);

                List<string> conditions = new List<string>();

                // Most probably a satellite
                ISatellite targetSat = RTCore.Instance.Network[target];
                if (targetSat != null) {
                    if (!RangeModelExtensions.HasLineOfSightWith(mySat, targetSat)) {
                        conditions.Add("No LOS");
                        error = true;
                    }

                    double dist    = RangeModelExtensions.DistanceTo(mySat, targetSat);
                    // Only standard model supported for now, RangeModel isn't designed for this problem
                    double maxDist = Math.Max(antenna.Omni, antenna.Dish);
                    conditions.Add(String.Format("{0}/{1}", 
                        RTUtil.FormatSI(dist, "m"), 
                        RTUtil.FormatSI(maxDist, "m")));
                    if (dist > maxDist) {
                        error = true;
                    }
                }

                try {
                    CelestialBody targetPlanet = RTCore.Instance.Network.Planets[target];
                    double dist = Vector3d.Distance(mySat.Position, targetPlanet.position);
                    double maxDist = Math.Max(antenna.Omni, antenna.Dish);
                    double spread = 2.0 * dist * Math.Sqrt(1-antenna.CosAngle*antenna.CosAngle);
                    int numTargets = countInCone(antenna, target);
                    
                    if (spread < 2.0 * targetPlanet.Radius) {
                        conditions.Add("Small Cone");
                        warning = true;
                    }

                    if (dist <= maxDist) {
                        conditions.Add(String.Format("{0}/{1} ({2} beam covers {3} targets)", 
                            RTUtil.FormatSI(dist, "m"), 
                            RTUtil.FormatSI(maxDist, "m"),
                            RTUtil.FormatSI(spread, "m"),
                            numTargets
                        ));
                    } else {
                        conditions.Add(String.Format("{0}/{1}", 
                            RTUtil.FormatSI(dist, "m"), 
                            RTUtil.FormatSI(maxDist, "m")
                        ));
                        error = true;
                    }
                    if (numTargets <= 0) {
                        warning = true;
                    }

                } catch (KeyNotFoundException) {}

                return new KeyValuePair<string, UnityEngine.Color>(
                    String.Join("; ", conditions.ToArray()), 
                    error ? UnityEngine.Color.red : (warning ? UnityEngine.Color.yellow : UnityEngine.Color.white)
                );
            }

            // Default behavior
            return new KeyValuePair<string, UnityEngine.Color>("", UnityEngine.Color.white);
        }
    }
}

