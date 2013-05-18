using NUnit.Framework;
using System;
using System.Collections.Generic;
using RemoteTech;

namespace Tests
{
    [TestFixture]
    public class PriorityQueueTest {

        public class Node : IComparable {
            public int Priority { get; set; }

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
                return "Priority = " + Priority;
            }

        }
        [Test]
        public void TestEnqueueDequeue() {
            PriorityQueue<Node> testQueue = new PriorityQueue<Node>();
            List<Node> testList = new List<Node>();
            Random rng = new Random(42);

            for (int i = 0; i < 100; i++) {
                Node n = new Node(rng.Next());
                testList.Add(n);
                testQueue.Enqueue(n);
            }

            testList.Sort();

            for (int i = 0; i < 100; i++) {
                Assert.AreEqual(testList[i].Priority, testQueue.Dequeue().Priority);
            }
        }

        [Test]
        public void TestIncreaseDecrease() {
            PriorityQueue<Node> testQueue = new PriorityQueue<Node>();
            List<Node> testList = new List<Node>();

            Random rng = new Random(42);

            for (int i = 0; i < 100; i++) {
                Node n = new Node(rng.Next());
                testList.Add(n);
                testQueue.Enqueue(n);
            }

            testList.Sort();

            for (int i = 0; i < 100; i++) {
                int old_priority = testList[i].Priority;
                testList[i].Priority = rng.Next();
                if (testList[i].Priority.CompareTo(old_priority) < 0) {
                    testQueue.Increase(testList[i]);
                } else {
                    testQueue.Decrease(testList[i]);
                }
            }

            testList.Sort();

            for (int i = 0; i < 100; i++) {
                Assert.AreEqual(testList[i].Priority, testQueue.Dequeue().Priority);
            }
            
        }
    }
}

