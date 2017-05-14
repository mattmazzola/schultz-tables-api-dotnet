using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SchultzTablesService.Options;

namespace SchultzTablesService.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class UsersController : Controller
    {
        private readonly DocumentDbOptions documentDbOptions;

        public UsersController(IOptions<DocumentDbOptions> documentDbOptions)
        {
            this.documentDbOptions = documentDbOptions.Value;
        }

        // GET: api/Users
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new string[] { "value1", "value2", documentDbOptions.AccountKey, documentDbOptions.AccountUri });
        }

        // GET: api/Users/5
        [HttpGet("{id}", Name = "Get")]
        public IActionResult Get(int id)
        {
            return Ok("value");
        }
        
        // POST: api/Users
        [HttpPost]
        public void Post([FromBody]string value)
        {
        }
        
        // PUT: api/Users/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }
        
        // DELETE: api/ApiWithActions/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
