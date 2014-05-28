using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTech
{
    public class GroupManager : IEnumerable<Group>
    {
        public IEnumerable<ISatellite> this[String name] { get { return For(name); } }
        public IEnumerable<Group> Groups { get { return groupCache.Keys; } }
        public void RemoveGroup(Group group)
        {
            if (groupCache.ContainsKey(group))
            {
                foreach (var sat in groupCache[group])
                {
                    sat.Group = new Group(String.Empty);
                }
                groupCache.Remove(group);
            }
        }
        public Group Register(ISatellite sat, Group group)
        {
            var currentGroup = sat.Group;
            if (groupCache.ContainsKey(currentGroup))
            {
                groupCache[currentGroup].Remove(sat);
            }
            if (!groupCache.ContainsKey(group))
            {
                groupCache[group] = new List<ISatellite>();
            }
            groupCache[group].Add(sat);
            return group;
        }

        private Dictionary<Group, IList<ISatellite>> groupCache = new Dictionary<Group, IList<ISatellite>>();
        private IEnumerable<ISatellite> For(Group group)
        {
            return groupCache.ContainsKey(group) ? groupCache[group] : Enumerable.Empty<ISatellite>();
        }

        private IEnumerable<ISatellite> For(String group)
        {
            return For(new Group(group));
        }

        public IEnumerator<Group> GetEnumerator()
        {
            return Groups.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
