using NUnit.Framework;
using System;
using System.Collections.Generic;
using RemoteTech;

namespace Tests
{
    [TestFixture()]
    public class PathfinderTest {

        public int[][] TestMatrix1 = new int[][] {
            new int[]{0, 3, 0, 0, 0, 0, 0, 0},
            new int[]{3, 0, 3, 0, 0, 0, 0, 0},
            new int[]{0, 3, 0, 3, 0, 0, 0, 0},
            new int[]{0, 0, 3, 0, 3, 0, 0, 0},
            new int[]{0, 0, 0, 3, 0, 3, 0, 1},
            new int[]{0, 0, 0, 0, 3, 0, 3, 0},
            new int[]{0, 0, 0, 0, 0, 3, 0, 3},
            new int[]{0, 0, 0, 0, 0, 0, 3, 0},
        };

        public List<int> TestExpected1 = new List<int> {0,1,2,3,4,7};

        public class Scenario {

            public int[][] AdjacencyMatrix { get; private set; }

            public Scenario(int[][] matrix) {
                this.AdjacencyMatrix = matrix;
            }

            public Pair<List<int>, float> Run() {
                return Pathfinder.Solve<int>(0, AdjacencyMatrix.Length - 1, new Pathfinder.NeighbourDelegate<int>(FindNeighbours),
                                                                     new Pathfinder.CostDelegate<int>(FindCost),
                                                                     new Pathfinder.HeuristicDelegate<int>(FindHeuristic));
            }

            public List<int> FindNeighbours(int node) {
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
            List<int> firstPath = first.Run().First;
            for (int i = 0; i < TestExpected1.Count; i++) {
                Assert.AreEqual(TestExpected1[i], firstPath[i]);
            }
        }

        [Test()]
        public void TestGraphGen() {

        }

    }
}

