using System;
using Rhino.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RemoteTech
{
    [TestClass]
    public class SatelliteManagerTests
    {
        [ClassInitialize]
        public void Init()
        {
            var mockDataSource = MockRepository.GenerateMock<Vessel>();
        }

        [TestMethod]
        public void TestMethod1()
        {
        }
    }
}
