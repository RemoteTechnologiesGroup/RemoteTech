using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Andvari
{
    [TestClass]
    public class ServiceContainerTests
    {
        private interface IA {}
        private interface IB {}
        private interface IC {}
        private interface ID {}
        private interface IE {}
        private class A : IA
        {
            private IB b;
            private IC c;
            public A(IB b, IC c) 
            {
                this.b = b;
                this.c = c;

                Trace.WriteLine("A");
            }

            public override String ToString()
            {
                return String.Format("A({0},{1})", b, c);
            }
        }

        private class B : IB
        {
            private ID d;
            private IE e;
            public B(ID d, IE e) 
            {
                this.d = d;
                this.e = e;

                Trace.WriteLine("B");
            }

            public override String ToString()
            {
                return String.Format("B({0},{1})", d, e);
            }
        }

        private class C : IC
        {
            public C() 
            {
                Trace.WriteLine("C");
            }
            public override String ToString()
            {
                return String.Format("C");
            }
        }

        private class D : ID
        {
            public D() 
            {
                Trace.WriteLine("D");
            }
            public override String ToString()
            {
                return String.Format("D");
            }
        }

        private class E : IE
        {
            public E()
            {
                Trace.WriteLine("E");
            }
            public override String ToString()
            {
                return String.Format("E");
            }
        }

        private ServiceContainer container;

        [TestInitialize]
        public void Setup()
        {
            container = new ServiceContainer();
        }

        [TestCleanup]
        public void Dispose()
        {
            container.Dispose();
            container = null;
        }
        [TestMethod]
        public void TestRegister()
        {
            container.RegisterAll(
                container.NewService<A>(typeof(IA)).AsSingleton(),
                container.NewService<B>(typeof(IB)).AsSingleton(),
                container.NewService<C>(typeof(IC)).AsSingleton(),
                container.NewService<D>(typeof(ID)).AsSingleton(),
                container.NewService<E>(typeof(IE)).AsSingleton()
                );

            var a = container.GetInstance<IA>();
            var b = container.GetInstance<IB>();
            var c = container.GetInstance<IC>();
            var d = container.GetInstance<ID>();
            var e = container.GetInstance<IE>();

            Assert.AreEqual("A(B(D,E),C)", a.ToString());
            Assert.AreEqual("B(D,E)", b.ToString());
            Assert.AreEqual("C", c.ToString());
            Assert.AreEqual("D", d.ToString());
            Assert.AreEqual("E", e.ToString());
        }

        [TestMethod]
        public void Test_GetInstance_Throw_NotAnInterfaceException_On_Implementation()
        {
            bool thrown = false;
            try
            {
                container.GetInstance<A>();
            }
            catch (NotAnInterfaceException)
            {
                thrown = true;
            }
            catch (Exception e)
            {
                Trace.Write(e);
            }

            Assert.AreEqual(true, thrown, "ServiceContainer did not throw NotAnInterfaceException");
        }
    }
}
