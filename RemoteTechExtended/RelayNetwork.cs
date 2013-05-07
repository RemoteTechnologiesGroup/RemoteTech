using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
{

    public class RelayNetwork
    {
        public List<RelayNode>
            all,
            comSats,
            commandStations;

        public RelayNetwork()
        {
            all = new List<RelayNode>();
            comSats = new List<RelayNode>();
            commandStations = new List<RelayNode>();


            foreach (Vessel v in FlightGlobals.Vessels)
            {
                if (RTUtils.IsComsat(v)) all.Add(new RelayNode(v));
            }

            foreach (RelayNode node in all)
            {
                if (node.HasCommand) commandStations.Add(node);
                else comSats.Add(node);
            }
            all.Add(new RelayNode());
            commandStations.Add(new RelayNode());

            foreach (KeyValuePair<Vessel, RemoteCore> pair in RTGlobals.coreList)
            {
                try
                {
                    pair.Value.Rnode = new RelayNode(pair.Key);
                }
                catch { }
            }

        }

        public void Reload()
        {
            foreach (RelayNode node in all) node.Reload();
            foreach (RelayNode node in comSats) node.Reload();
            foreach (RelayNode node in commandStations) node.Reload();
        }

        public void Reload(RelayNode reloadNode)
        {
            foreach (RelayNode node in all) if (node.Equals(reloadNode)) node.Reload();

            foreach (RelayNode node in comSats) if (node.Equals(reloadNode)) node.Reload();

            foreach (RelayNode node in commandStations) if (node.Equals(reloadNode)) node.Reload();
        }

        public RelayPath GetCommandPath(RelayNode start)
        {
            double compare = double.MaxValue;
            RelayPath output = null;
            foreach (RelayNode node in commandStations)
            {
                if (!start.Equals(node) && node.HasCommand)
                {
                    RelayPath tmp = findShortestRelayPath(start, node);
                    if (tmp != null && tmp.Length < compare)
                    {
                        output = tmp;
                        compare = tmp.Length;
                    }
                }
            }
            return output;
        }

        public bool inContactWith(RelayNode node, RelayNode other)
        {
            return (findShortestRelayPath(node, other) != null);
        }

        RelayPath findShortestRelayPath(RelayNode fromNode, RelayNode toNode)
        {
            RelayNode goal = toNode.Vessel == null ? new RelayNode() : new RelayNode(toNode.Vessel);

            RelayNode start = new RelayNode(fromNode.Vessel);

            HashSet<RelayNode> closedSet = new HashSet<RelayNode>();
            HashSet<RelayNode> openSet = new HashSet<RelayNode>();

            Dictionary<RelayNode, RelayNode> cameFrom = new Dictionary<RelayNode, RelayNode>();
            Dictionary<RelayNode, double> gScore = new Dictionary<RelayNode, double>();
            Dictionary<RelayNode, double> hScore = new Dictionary<RelayNode, double>();
            Dictionary<RelayNode, double> fScore = new Dictionary<RelayNode, double>();

            openSet.Add(start);

            double startBaseHeuristic = (start.Position - goal.Position).magnitude;
            gScore[start] = 0.0;
            hScore[start] = startBaseHeuristic;
            fScore[start] = startBaseHeuristic;


            HashSet<RelayNode> neighbors = new HashSet<RelayNode>(all);
            neighbors.Add(start);
            neighbors.Add(goal);

            RelayPath path = null;
            while (openSet.Count > 0)
            {
                RelayNode current = null;
                double currentBestScore = double.MaxValue;
                foreach (KeyValuePair<RelayNode, double> pair in fScore)
                {
                    if (openSet.Contains(pair.Key) && pair.Value < currentBestScore)
                    {
                        current = pair.Key;
                        currentBestScore = pair.Value;
                    }
                }
                if (current == goal)
                {
                    path = new RelayPath(reconstructPath(cameFrom, goal));
                    break;
                }
                openSet.Remove(current);
                closedSet.Add(current);
                foreach (RelayNode neighbor in neighbors)
                {
                    if (!closedSet.Contains(neighbor) && inRange(neighbor, current) && lineOfSight(neighbor, current))
                    {
                        //double tentGScore = gScore[current] - (neighbor.Position - current.Position).magnitude;
                        double tentGScore = gScore[current] + (neighbor.Position - current.Position).magnitude;

                        bool tentIsBetter = false;
                        if (!openSet.Contains(neighbor))
                        {
                            openSet.Add(neighbor);
                            hScore[neighbor] = (neighbor.Position - goal.Position).magnitude;
                            tentIsBetter = true;
                        }
                        else if (tentGScore < gScore[neighbor])
                        {
                            tentIsBetter = true;
                        }

                        if (tentIsBetter)
                        {
                            cameFrom[neighbor] = current;
                            gScore[neighbor] = tentGScore;
                            fScore[neighbor] = tentGScore + hScore[neighbor];
                        }
                    }
                }

            }

            return path;
        }

        List<RelayNode> reconstructPath(Dictionary<RelayNode, RelayNode> cameFrom, RelayNode curNode)
        {
            List<RelayNode> tmp = null;
            if (cameFrom.ContainsKey(curNode))
            {
                tmp = reconstructPath(cameFrom, cameFrom[curNode]);
                tmp.Add(curNode);
            }
            else
            {
                tmp = new List<RelayNode>() { curNode };
            }
            return tmp;
        }

        bool inRange(RelayNode na, RelayNode nb)
        {

            if (CheatOptions.InfiniteEVAFuel)
                return true;

            float distance = (float)(na.Position - nb.Position).magnitude / 1000;

            if (na.HasAntenna && nb.HasAntenna && na.AntennaRange >= distance && nb.AntennaRange >= distance) { return true; }
            if (na.HasDish && nb.HasAntenna && ((nb.AntennaRange * 2) >= distance))
            {
                foreach (DishData naData in na.DishData)
                {
                    if (((naData.pointedAt.Equals(nb.Orbits) && !na.Orbits.Equals(nb.Orbits)) || naData.pointedAt.Equals(nb.ID)) && (naData.dishRange >= distance)) { return true; }
                }
            }

            if (nb.HasDish && na.HasAntenna && ((na.AntennaRange * 2) >= distance))
            {
                foreach (DishData nbData in nb.DishData)
                {
                    if (((nbData.pointedAt.Equals(na.Orbits) && !nb.Orbits.Equals(na.Orbits)) || nbData.pointedAt.Equals(na.ID)) && (nbData.dishRange >= distance)) { return true; }
                }
            }

            if (na.HasDish && nb.HasDish)
            {

                bool aDish = false;
                bool bDish = false;
                foreach (DishData naData in na.DishData)
                {
                    if (((naData.pointedAt.Equals(nb.Orbits) && !na.Orbits.Equals(nb.Orbits)) || naData.pointedAt.Equals(nb.ID)) && (naData.dishRange >= distance)) { aDish = true; break; }
                }
                foreach (DishData nbData in nb.DishData)
                {
                    if (((nbData.pointedAt.Equals(na.Orbits) && !nb.Orbits.Equals(na.Orbits)) || nbData.pointedAt.Equals(na.ID)) && (nbData.dishRange >= distance)) { bDish = true; break; }
                }

                return aDish && bDish;
            }

            return false;

        }

        bool lineOfSight(RelayNode na, RelayNode nb)
        {
            if (CheatOptions.InfiniteEVAFuel)
                return true;

            Vector3d a = na.Position;
            Vector3d b = nb.Position;
            foreach (CelestialBody referenceBody in FlightGlobals.Bodies)
            {
                Vector3d bodyFromA = referenceBody.position - a;
                Vector3d bFromA = b - a;
                if (Vector3d.Dot(bodyFromA, bFromA) > 0)
                {
                    Vector3d bFromAnorm = bFromA.normalized;
                    if (Vector3d.Dot(bodyFromA, bFromAnorm) < bFromA.magnitude)
                    { // check lateral offset from line between b and a
                        Vector3d lateralOffset = bodyFromA - Vector3d.Dot(bodyFromA, bFromAnorm) * bFromAnorm;
                        if (lateralOffset.magnitude < (referenceBody.Radius - 5))
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }


        void print(String s)
        {
            MonoBehaviour.print(s);
        }
    }
}
