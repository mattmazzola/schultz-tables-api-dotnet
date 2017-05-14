using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SchultzTablesService.Options;
using Microsoft.Azure.Documents.Client;

namespace SchultzTablesService.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class UsersController : Controller
    {
        private readonly DocumentDbOptions documentDbOptions;
        private readonly DocumentClient documentClient;

        public UsersController(IOptions<DocumentDbOptions> documentDbOptions, DocumentClient documentClient)
        {
            this.documentDbOptions = documentDbOptions.Value;
            this.documentClient = documentClient;
        }

        // GET: api/Users
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var users = documentClient.CreateDocumentQuery<Documents.User>(UriFactory.CreateDocumentCollectionUri(documentDbOptions.DatabaseName, documentDbOptions.UsersCollectionName)).ToList();

            return Ok(users);
        }

        // GET: api/Users/5
        [HttpGet("{id}", Name = "Get")]
        public IActionResult Get(string id)
        {
            var users = documentClient.CreateDocumentQuery<Documents.User>(UriFactory.CreateDocumentCollectionUri(documentDbOptions.DatabaseName, documentDbOptions.UsersCollectionName))
                .Where(user => user.Id == id)
                .ToList();

            if (users.Count == 0)
            {
                return NotFound($"User with id {id} was not found");
            }

            return Ok(users.First());
        }
        
        // POST: api/Users
        [HttpPost]
        public async Task<IActionResult> Post([FromBody]Documents.User user)
        {
            var newUser = await documentClient.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(documentDbOptions.DatabaseName, documentDbOptions.UsersCollectionName), user);

            return CreatedAtRoute("Get", new { id = user.Id }, newUser);
        }
        
        // PUT: api/Users/5
        [HttpPut("{id}")]
        public IActionResult Put(int id, [FromBody]string value)
        {
            return Forbid();
        }
        
        // DELETE: api/ApiWithActions/5
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            return Forbid();
        }
    }
}
