//using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR;
using Quizr.API.Hubs;

namespace Quizr.API.Services
{
    public interface IQuizrHubContextService
    {
        IHubContext<QuizrHub, IQuizrClient> GetQuizrHubContext();
    }
}
