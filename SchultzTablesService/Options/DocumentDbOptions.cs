using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SchultzTablesService.Options
{
    public class DocumentDbOptions
    {
        public string AccountUri { get; set; }
        public string AccountKey { get; set; }
        public string DatabaseName { get; set; }
        public string UsersCollectionName { get; set; }
    }
}
