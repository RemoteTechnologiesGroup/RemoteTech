using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RemoteTech
{
    [TestClass]
    public class AntennaMixinTestBench
    {
        public ConfigNode configNode;

        [TestInitialize]
        public void Init()
        {
            configNode = new ConfigNode("Antenna");
            configNode.AddValue("ActiveDishRange", 100);
            configNode.AddValue("ActiveOmniRange", 100);
            configNode.AddValue("Consumption", 2);
            configNode.AddValue("DishAngle", 45);
        }
        [TestMethod]
        public void TestLoad()
        {
            var mixin = new AntennaMixin(
                () => null,
                () => "MyName",
                (s, d) => { return d; }
            );

            mixin.Load(configNode);

            return;
        }
    }
}
