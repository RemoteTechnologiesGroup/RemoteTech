using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace RemoteTech
{
    [TestFixture]
    public class PriorityQueueTest {

        public const int Length = 100;

        public class Node : IComparable {
            public readonly int Priority;

            public Node(int priority) {
                this.Priority = priority;
            }

            public int CompareTo(Object obj) {
                if (obj == null) {
                    return 1;
                }
                Node node = obj as Node;
                if (node == null) {
                    throw new ArgumentException();
                }
                return Priority.CompareTo(node.Priority);
            }

            public override String ToString() {
                return "Node(" + Priority + ")";
            }

        }
        [Test]
        public void TestEnqueueDequeue() {
            PriorityQueue<Node> testQueue = new PriorityQueue<Node>();
            List<Node> testList = new List<Node>();
            Random rng = new Random();

            for (int i = 0; i < Length; i++) {
                Node n = new Node(rng.Next());
                testList.Add(n);
                testQueue.Enqueue(n);
            }
            testList.Sort();
            for (int i = 0; i < Length; i++) {
                Assert.AreSame(testList[i], testQueue.Dequeue(), "At ID: " + i);
            }
        }
    }
}

