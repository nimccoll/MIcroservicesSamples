using CalcFunctionsCSharp;
using CalcModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;

namespace CalcFunctionsTest
{
    [TestClass]
    public class CalcFunctionsTest
    {
        [TestMethod]
        public async Task FirstCalculationTest()
        {
            OutputModel expected = new OutputModel()
            {
                IntegerOutput = new int[] { 8 }
            };
            InputModel input = new InputModel()
            {
                IntegerInput = new int[] { 3, 5 },
                StringInput = new string[] { "Add", "Integer" }
            };

            Mock<HttpRequest> httpRequest = CreateMockRequest(input);
            IActionResult actualResult = await FirstCalculation.Run(httpRequest.Object, new Mock<ILogger>().Object);
            Assert.IsInstanceOfType(actualResult, typeof(OkObjectResult));
            if (actualResult is OkObjectResult)
            {
                Assert.IsInstanceOfType(((OkObjectResult)actualResult).Value, typeof(OutputModel));
                if (((OkObjectResult)actualResult).Value is OutputModel)
                {
                    OutputModel actual = (OutputModel)((OkObjectResult)actualResult).Value;
                    Assert.IsNull(actual.DecimalOutput);
                    Assert.IsNull(actual.StringOutput);
                    Assert.AreEqual(1, actual.IntegerOutput.Length);
                    Assert.AreEqual(expected.IntegerOutput[0], actual.IntegerOutput[0]);
                }
            }
        }

        private static Mock<HttpRequest> CreateMockRequest(object body)
        {
            var ms = new MemoryStream();
            var sw = new StreamWriter(ms);

            var json = JsonConvert.SerializeObject(body);

            sw.Write(json);
            sw.Flush();

            ms.Position = 0;

            var mockRequest = new Mock<HttpRequest>();
            mockRequest.Setup(x => x.Body).Returns(ms);

            return mockRequest;
        }
    }
}
