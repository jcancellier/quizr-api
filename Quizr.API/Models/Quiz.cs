using System.Collections.Generic;

namespace Quizr.API.Models
{
    public class Quiz
    {
        public string Name { get; set; }
        public IList<Question> Questions { get; set; }
    }
}
