using CspFoundation.Commons;
using CspFoundation.Models;
using CspFoundation.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Text;

namespace CspFoundation.Controllers
{
    [ApiController]
    [Route("azdo-cron")]
    public class AzDoCronTimerController : ControllerBase
    {
        private readonly ILogger<AzDoCronTimerController> Logger;
        private readonly IMainService MainService;
        private readonly AppSettingsModel AppSettings;
        private readonly SecretSettingsModel SecretSettings;

        public AzDoCronTimerController(
            ILogger<AzDoCronTimerController> logger,
            IMainService mainService,
            IOptions<AppSettingsModel> optionsSettingsAccessor,
            IOptions<SecretSettingsModel> optionsKeyVaultAccessor
            )
        {
            this.Logger = logger;
            this.MainService = mainService;
            this.AppSettings = optionsSettingsAccessor.Value;
            this.SecretSettings = optionsKeyVaultAccessor.Value;
        }

        [HttpGet]
        public IActionResult DebugAsyncTrigger()
        {
            this.Logger.LogInformation("Method:{Method};Message:-;Status:Start;", MethodHelper.GetCurrentMethod());

            var responseMessage = new StringBuilder();
            responseMessage.AppendLine("Welcome to Container Apps!");
            responseMessage.AppendLine("SecretSettingsModel!!->Azure Key Vault");
            responseMessage.AppendLine(JsonConvert.SerializeObject(this.SecretSettings, Formatting.Indented));
            responseMessage.AppendLine("");
            responseMessage.AppendLine("AppSettingsModel!!->appsettings.json");
            responseMessage.AppendLine(JsonConvert.SerializeObject(this.AppSettings, Formatting.Indented));

            this.Logger.LogInformation("Method:{Method};Message:-;Status:End;", MethodHelper.GetCurrentMethod());

            return new OkObjectResult(responseMessage.ToString());
        }


        [HttpPost]
        public async Task<IActionResult> PostAsync()
        {
            this.Logger.LogInformation("azdo-cron triggered at: {time}", DateTime.Now);

            var returnModel = new ReturnModel();
            try
            {
                await this.MainService.ExecuteOnCronAsync();
            }
            catch (Exception ex)
            {
                returnModel.IsSucceed = false;
                returnModel.Exception = ex.ToString();
                this.Logger.LogError($"azdo-cron error: {ex}");
            }

            return new ContentResult
            {
                Content = JsonConvert.SerializeObject(returnModel),
                ContentType = "application/json; charset=utf-8",
            };
        }
    }

}
