using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace RemoteTech
{
    [TestFixture()]
    public class PathfinderTest {

        public int[][] TestMatrix1 = new int[][] {
            new int[]{0, 1, 0, 0, 0, 0, 0, 0},
            new int[]{0, 0, 1, 0, 0, 0, 0, 0},
            new int[]{0, 0, 0, 1, 0, 0, 0, 0},
            new int[]{0, 0, 0, 0, 1, 0, 0, 0},
            new int[]{0, 0, 0, 0, 0, 1, 0, 0},
            new int[]{0, 0, 0, 0, 0, 0, 1, 0},
            new int[]{0, 0, 0, 0, 0, 0, 0, 1},
            new int[]{0, 0, 0, 0, 0, 0, 0, 0},
        };
        public List<int> TestExpected1 = new List<int> {0,1,2,3,4,5,6,7};

        public class Scenario {

            public int[][] AdjacencyMatrix { get; private set; }

            public Scenario(int[][] matrix) {
                this.AdjacencyMatrix = matrix;
            }

            public IList<int> Run() {
                float cost;
                return Pathfinder.Solve<int>(0, AdjacencyMatrix.Length - 1, out cost, new Pathfinder.NeighbourDelegate<int>(FindNeighbours),
                                                                     new Pathfinder.CostDelegate<int>(FindCost),
                                                                     new Pathfinder.HeuristicDelegate<int>(FindHeuristic));
            }

            public IList<int> FindNeighbours(int node) {
                List<int> neighbours = new List<int>();
                for (int i = 0; i < AdjacencyMatrix[node].Length; i++) {
                    if (AdjacencyMatrix[node][i] != 0) {
                        neighbours.Add(i);
                    }
                }
                System.Diagnostics.Debug.Write("FindNeighbours: " + neighbours.ToString());
                return neighbours;
            }

            public float FindCost(int a, int b) {
                return AdjacencyMatrix[a][b];
            }

            public float FindHeuristic(int a, int goal) {
                return 0;
            }

        }


        [Test()]
        public void TestPathfinder() {
            Scenario first = new Scenario(TestMatrix1);
            IList<int> firstPath = first.Run();
            for (int i = 0; i < TestExpected1.Count; i++) {
                Assert.Equals(firstPath[i], TestExpected1[i]);
            }
        }

        [Test()]
        public void TestGraphGen() {

        }
    }
}

