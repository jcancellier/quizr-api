using System.Threading.Tasks;
using Quizr.API.Models;

namespace Quizr.API.Hubs
{
    public interface IQuizrClient
    {
        Task UpdateRoomTimer(int time);
        Task UpdateRoomPhase(string phase);
        Task UpdateQuizRoomUsers(int userCount);
        Task ReceiveQuizResults(object quizResults);
        Task ReceiveNewQuestion(object question);
    }
}
