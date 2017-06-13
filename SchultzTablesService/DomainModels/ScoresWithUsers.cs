using System.Collections.Generic;

namespace SchultzTablesService.DomainModels
{
    public class ScoresWithUsers
    {
        public List<Score> Scores { get; set; }
        public List<User> Users { get; set; }
    }
}
