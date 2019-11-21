using Microsoft.AspNetCore.SignalR;
using Quizr.API.Hubs;

namespace Quizr.API.Services
{
    public class QuizrHubContextService : IQuizrHubContextService
    {
        private IHubContext<QuizrHub, IQuizrClient> _quizrHubContext { get; set; }

        public QuizrHubContextService(IHubContext<QuizrHub, IQuizrClient> quizrHubContext)
        {
            _quizrHubContext = quizrHubContext;
        }

        public IHubContext<QuizrHub, IQuizrClient> GetQuizrHubContext()
        {
            return _quizrHubContext;
        }
    }
}
