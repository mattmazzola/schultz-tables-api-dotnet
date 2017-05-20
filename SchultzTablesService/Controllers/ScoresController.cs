using System;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SchultzTablesService.Documents;
using SchultzTablesService.Options;

namespace SchultzTablesService.Controllers
{
    [Authorize]
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class ScoresController : Controller
    {
        private readonly DocumentDbOptions documentDbOptions;
        private readonly DocumentClient documentClient;
        private readonly ILogger logger;

        public ScoresController(IOptions<DocumentDbOptions> documentDbOptions, DocumentClient documentClient, ILogger<ScoresController> logger)
        {
            this.documentDbOptions = documentDbOptions.Value;
            this.documentClient = documentClient;
            this.logger = logger;
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

        // GET: api/scores/start
        [HttpGet("start")]
        public async Task<IActionResult> Start()
        {
            var timeId = new TimeId();
            var newTimeId = await documentClient.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(documentDbOptions.DatabaseName, documentDbOptions.TimeIdsCollectionName), timeId);

            return Ok(newTimeId.Resource.Id);
        }

        // POST: api/scores
        [HttpPost]
        public async Task<IActionResult> Post([FromBody]Documents.ScoreInput scoreInput)
        {
            var now = DateTimeOffset.UtcNow;

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Find matching time id
            var timeIds = documentClient.CreateDocumentQuery<Documents.TimeId>(UriFactory.CreateDocumentCollectionUri(documentDbOptions.DatabaseName, documentDbOptions.TimeIdsCollectionName))
                .Where(timeId => timeId.Id == scoreInput.TimeId)
                .ToList();

            // If no matching time id was found log warning with user token to review later for possible cheating.
            if (timeIds.Count == 0)
            {
                logger.LogWarning(LoggingEvents.TIMEID_NOTFOUND, $"User may be attempting to cheat. Review user account with id: ");
                return StatusCode((int)HttpStatusCode.Unauthorized, $"You have been logged for attempted cheating.  Your account will be reviewed and may be deleted.");
            }

            var matchingTimeId = timeIds.First();

            // Delete matching id
            await documentClient.DeleteDocumentAsync(UriFactory.CreateDocumentUri(documentDbOptions.DatabaseName, documentDbOptions.TimeIdsCollectionName, matchingTimeId.Id));

            // Time data is not value log warning with user token for review later.
            var isTimeDataValid = IsTimeDataValid(
                matchingTimeId.Time,
                now,
                scoreInput,
                TimeSpan.FromSeconds(10)
                );

            if (!isTimeDataValid)
            {
                return StatusCode((int)HttpStatusCode.Unauthorized, $"You have been logged for attempted cheating.  Your account will be reviewed and may be deleted.");
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

        private bool IsTimeDataValid(
            DateTimeOffset serverStartTime,
            DateTimeOffset serverEndTime,
            ScoreInput scoreInput,
            TimeSpan maxTimeSpan
            )
        {
            if (scoreInput.EndTime < scoreInput.StartTime)
                return false;

            var startTimeSpan = serverStartTime - scoreInput.StartTime;
            if (startTimeSpan.Duration() > maxTimeSpan)
                return false;

            var endTimeSpan = serverEndTime - scoreInput.EndTime;
            if (endTimeSpan.Duration() > maxTimeSpan)
                return false;

            var durationDifference = (serverEndTime - serverStartTime) - (scoreInput.EndTime - scoreInput.StartTime);
            if (durationDifference.Duration() > maxTimeSpan)
                return false;

            var anyAnwsersOutsideOfSubmissionRange = scoreInput.UserSequence
                .Select(s => s.Time)
                .Any(time => (time < scoreInput.StartTime) || (time > scoreInput.EndTime));

            if (anyAnwsersOutsideOfSubmissionRange)
                return false;

            return true;
        }
    }
}
