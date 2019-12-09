using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Quizr.API.Models
{
    public class User
    {
        public string Name { get; set; }
        public string ConnectionId { get; set; }
        public int Score { get; set; }
        public int CurrentQuestionAnswer { get; set; }
    }
}
