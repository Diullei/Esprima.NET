using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Esprima.NET.Test
{
    [TestClass]
    public class BasicTest
    {
        [TestMethod]
        public void Test()
        {
            const string code = "var f = 3";
            var result = new Esprima().Parse(code).ToString();
            Assert.AreEqual("var f = 3; ", result);
        }
    }
}
