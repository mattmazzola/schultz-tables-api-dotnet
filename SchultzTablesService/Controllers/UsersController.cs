using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using SchultzTablesService.Options;
using SchultzTablesService.Models;

namespace SchultzTablesService.Controllers
{
    [Authorize]
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class UsersController : Controller
    {
        private readonly AadB2cApplicationOptions aadB2cApplicationOptions;
        private readonly AuthenticationContext authenticationContext;
        private readonly ClientCredential clientCredential;
        private readonly DocumentDbOptions documentDbOptions;
        private readonly DocumentClient documentClient;

        public UsersController(IOptions<AadB2cApplicationOptions> aadB2cApplicationOptions, IOptions<DocumentDbOptions> documentDbOptions, DocumentClient documentClient)
        {
            this.aadB2cApplicationOptions = aadB2cApplicationOptions.Value;
            this.documentDbOptions = documentDbOptions.Value;
            this.documentClient = documentClient;

            this.authenticationContext = new AuthenticationContext($"https://login.microsoftonline.com/{aadB2cApplicationOptions.Value.Tenant}");
            this.clientCredential = new ClientCredential(aadB2cApplicationOptions.Value.ApplicationId, aadB2cApplicationOptions.Value.ApplicationKey);
        }

        // GET: api/Users
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var authenticationResult = await authenticationContext.AcquireTokenAsync("https://graph.windows.net", clientCredential);

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, $"https://graph.windows.net/{aadB2cApplicationOptions.Tenant}/users?api-version=1.6");
            httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue(authenticationResult.AccessTokenType, authenticationResult.AccessToken);

            var httpClient = new HttpClient();
            var response = await httpClient.SendAsync(httpRequestMessage);
            var users = JsonConvert.DeserializeObject<OdataResponse<IList<object>>>(await response.Content.ReadAsStringAsync());

            return Ok(users.Value);
        }

        // GET: api/Users/5
        [HttpGet("{id}", Name = "Get")]
        public async Task<IActionResult> Get(string id)
        {
            var authenticationResult = await authenticationContext.AcquireTokenAsync("https://graph.windows.net", clientCredential);

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, $"https://graph.windows.net/{aadB2cApplicationOptions.Tenant}/users/{id}?api-version=1.6");
            httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue(authenticationResult.AccessTokenType, authenticationResult.AccessToken);

            var httpClient = new HttpClient();
            var response = await httpClient.SendAsync(httpRequestMessage);
            var user = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());

            return Ok(user);
        }
    }
}
