using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTech
{
    public class Group : IConfigNode, IEquatable<Group>
    {
        public static Group Empty = new Group(String.Empty);
        public IEnumerable<ISatellite> Satellites { get { return RTCore.Instance.Groups[group]; } }

        [Persistent] private readonly String group;
        public Group(String group)
        {
            this.group = group;
        }

        public bool Equals(Group other)
        {
            return other.group.Equals(this.group);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            var group = obj as Group;
            if (group == null)
                return false;
            else
                return Equals(group);
        }

        public override int GetHashCode()
        {
            return group.GetHashCode();
        }

        public override String ToString()
        {
            return group;
        }

        void IConfigNode.Load(ConfigNode node)
        {
            ConfigNode.LoadObjectFromConfig(this, node);
        }

        void IConfigNode.Save(ConfigNode node)
        {
            ConfigNode.CreateConfigFromObject(this, 0, node);
        }
    }
}
