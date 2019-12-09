using System.Collections.Generic;

namespace Quizr.API.Models
{
    public class Question
    {
        public string Text { get; set; }
        public IEnumerable<string> Answers { get; set; }
        public int CorrectAnswerIndex { get; set; }
    }
}