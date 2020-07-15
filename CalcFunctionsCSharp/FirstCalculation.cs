using CalcModels;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CalcFunctionsCSharp
{
    public static class FirstCalculation
    {
        [FunctionName("FirstCalculation")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("FirstCalculation HTTP trigger function processed a request.");

            ActionResult result = null;
            InputModel model = null;
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            try
            {
                model = JsonConvert.DeserializeObject<InputModel>(requestBody);
            }
            catch { }

            if (model != null)
            {
                // Perform calculation logic here
                if (model.StringInput[0] == "Add")
                {
                    OutputModel outputModel = new OutputModel();
                    if (model.StringInput[1] == "Integer")
                    {
                        int calculationResult = model.IntegerInput[0] + model.IntegerInput[1];
                        outputModel.IntegerOutput = new int[] { calculationResult };
                    }
                    else
                    {
                        decimal calculationResult = model.DecimalInput[0] + model.DecimalInput[1];
                        outputModel.DecimalOutput = new decimal[] { calculationResult };
                    }
                    result = new OkObjectResult(outputModel);
                }
                else
                {
                    result = new BadRequestObjectResult("Invalid input. Only the Add operation is supported at this time.");
                }
            }
            else
            {
                result = new BadRequestObjectResult("Invalid input. Please post an InputModel object in the request body.");
            }

            return result;
        }
    }
}
