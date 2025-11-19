using CspFoundation.Commons;
using CspFoundation.Models;
using CspFoundation.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Text;

namespace CspFoundation.Api.Controllers
{
    [ApiController]
    [Route("azdo-stream")]
    public sealed class AzDoStreamController : ControllerBase
    {
        private readonly ILogger<AzDoStreamController> Logger;
        private readonly AppSettingsModel AppSettings;
        private readonly SecretSettingsModel SecretSettings;
        private readonly IMainService MainService;

        public AzDoStreamController(
            ILogger<AzDoStreamController> logger,
            IOptions<AppSettingsModel> optionsSettingsAccessor,
            IOptions<SecretSettingsModel> optionsKeyVaultAccessor,
            IMainService mainService)
        {
            this.Logger = logger;
            this.AppSettings = optionsSettingsAccessor.Value;
            this.SecretSettings = optionsKeyVaultAccessor.Value;
            this.MainService = mainService;
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync()
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var raw = await reader.ReadToEndAsync();

            this.Logger.LogInformation("Method:{Method};Message:-;Status:Start;", MethodHelper.GetCurrentMethod());
            var returnModel = new ReturnModel();
            try
            {
                await this.MainService.ExecuteAsync(raw);
            }
            catch (Exception ex)
            {
                returnModel.IsSucceed = false;
                returnModel.Exception = ex.ToString();
                this.Logger.LogError($"Method:{MethodHelper.GetCurrentMethod()};Message:{ex.ToString()};Status:Error;");
            }
            this.Logger.LogInformation("Method:{Method};Message:-;Status:End;", MethodHelper.GetCurrentMethod());

            return new ContentResult
            {
                Content = JsonConvert.SerializeObject(returnModel, Formatting.None),
                ContentType = "application/json; charset=utf-8",
                //StatusCode = 200
            };
        }

    }
}
