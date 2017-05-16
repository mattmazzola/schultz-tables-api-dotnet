using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SchultzTablesService.Options;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents;
using SchultzTablesService.Documents;
using System;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace SchultzTablesService.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class ScoresController : Controller
    {
        private readonly DocumentDbOptions documentDbOptions;
        private readonly DocumentClient documentClient;

        public ScoresController(IOptions<DocumentDbOptions> documentDbOptions, DocumentClient documentClient)
        {
            this.documentDbOptions = documentDbOptions.Value;
            this.documentClient = documentClient;
        }

        // GET: api/scores
        [HttpGet]
        public IActionResult Get()
        {
            var scores = documentClient.CreateDocumentQuery<Documents.Score>(UriFactory.CreateDocumentCollectionUri(documentDbOptions.DatabaseName, documentDbOptions.ScoresCollectionName)).ToList();

            return Ok(scores);
        }

        // GET: api/scores/5
        [HttpGet("{id}", Name = "GetScore")]
        public IActionResult Get(string id)
        {
            var scores = documentClient.CreateDocumentQuery<Documents.Score>(UriFactory.CreateDocumentCollectionUri(documentDbOptions.DatabaseName, documentDbOptions.ScoresCollectionName))
                .Where(score => score.Id == id)
                .ToList();

            if (scores.Count == 0)
            {
                return NotFound($"User with id {id} was not found");
            }

            return Ok(scores.First());
        }

        // POST: api/scores
        [HttpPost]
        public async Task<IActionResult> Post([FromBody]Documents.ScoreInput scoreInput)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var tableLayout = new TableLayout()
            {
                Width = scoreInput.TableWidth,
                Height = scoreInput.TableHeight,
                ExpectedSequence = scoreInput.ExpectedSequence,
                RandomizedSequence = scoreInput.RandomizedSequence
            };

            var tableLayoutString = JsonConvert.SerializeObject(tableLayout);

            var tableType = new TableType()
            {
                Width = scoreInput.TableWidth,
                Height = scoreInput.TableHeight,
                Properties = scoreInput.TableProperties.OrderBy(p => p.Key).ToList()
            };

            var tableTypeString = JsonConvert.SerializeObject(tableType);

            using (var hashAlgorithm = SHA256.Create())
            {
                var tableLayoutHash = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(tableLayoutString));
                tableLayout.Id = Convert.ToBase64String(tableLayoutHash).Replace('/', '_');

                var tableTypeHash = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(tableTypeString));
                tableType.Id = Convert.ToBase64String(tableTypeHash).Replace('/', '_');
            }

            var score = new Score()
            {
                Sequence = scoreInput.UserSequence,
                TableLayoutId = tableLayout.Id,
                TableTypeId = tableType.Id,
                StartTime = scoreInput.StartTime,
                EndTime = scoreInput.EndTime,
                Duration = scoreInput.Duration,
                UserId = scoreInput.UserId
            };

            var newTableLayout = await documentClient.UpsertDocumentAsync(UriFactory.CreateDocumentCollectionUri(documentDbOptions.DatabaseName, documentDbOptions.TableLayoutsCollectionName), tableLayout);
            var newTableType = await documentClient.UpsertDocumentAsync(UriFactory.CreateDocumentCollectionUri(documentDbOptions.DatabaseName, documentDbOptions.TableTypesCollectionName), tableType);
            var newScore = await documentClient.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(documentDbOptions.DatabaseName, documentDbOptions.ScoresCollectionName), score);
            var createdUrl = Url.RouteUrl("GetScore", new { id = newScore.Resource.Id });

            return Created(createdUrl, newScore.Resource);
        }
    }
}
