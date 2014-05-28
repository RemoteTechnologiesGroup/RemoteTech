using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTech
{
    public class Target : IPersistenceLoad, IEnumerable<ISatellite>, IEquatable<Target>
    {
        public static Target Empty = new Target();
        public bool IsMultiple { get { return Type != TargetType.Group; } }
        private enum TargetType {
            Single,
            ActiveVessel,
            Planet,
            Group,
            Empty,
        }

        [Persistent] private String Id;
        [Persistent] private TargetType Type;
        private Guid guid;
        private Target() { Type = TargetType.Empty; Id = String.Empty; }
        private Target(String id, TargetType type)
        {
            this.Id = id;
            this.Type = type;
            PersistenceLoad();
        }
        public IEnumerable<ISatellite> GetTargets()
        {
            switch (Type)
            {
                case TargetType.Empty:
                    yield break;
                case TargetType.Single:
                    var sat = RTCore.Instance.Satellites[guid];
                    if (sat != null) yield return sat;
                    yield break;
                case TargetType.Planet:
                    var planet = RTCore.Instance.Bodies[guid];
                    if (planet != null) yield return planet.AsSatellite();
                    yield break;
                case TargetType.ActiveVessel:
                    var act = RTCore.Instance.Satellites[RTCore.Instance.Vessels.ActiveVessel];
                    if (act != null) yield return act;
                    yield break;
                case TargetType.Group:
                    foreach (var i in RTCore.Instance.Groups[Id])
                    {
                        yield return i;
                    }
                    yield break;
            }
        }

        public bool Includes(ICelestialBody cb) {
            return Type == TargetType.Planet && cb.AsSatellite().Guid == guid;
        }
        public static Target SingleUnsafeCompatibility(Guid guid)
        {
            return new Target(guid.ToString(), TargetType.Single);
        }
        public static Target Single(ISatellite sat)
        {
            return new Target(sat.Guid.ToString(), TargetType.Single);
        }

        public static Target Planet(ICelestialBody cb)
        {
            return new Target(cb.AsSatellite().Guid.ToString(), TargetType.Planet);
        }

        public static Target Group(String name)
        {
            return new Target(name, TargetType.Group);
        }

        public static Target ActiveVessel()
        {
            return new Target(String.Empty, TargetType.ActiveVessel);
        }
        public void PersistenceLoad() {
            if (Type == TargetType.Single || Type == TargetType.Planet)
            {
                this.guid = new Guid(Id);
            }
        }
        public bool Equals(Target other)
        {
            return other.Type.Equals(this.Type) && other.Id.Equals(this.Id);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            var target = obj as Target;
            if (target == null)
                return false;
            else
                return Equals(target);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode() + Type.GetHashCode();
        }

        public IEnumerator<ISatellite> GetEnumerator()
        {
            return GetTargets().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
