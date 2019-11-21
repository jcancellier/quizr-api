using System.Threading.Tasks;

namespace Quizr.API.Hubs
{
    public interface IQuizrClient
    {
        Task UpdateRoomTimer(int time);
    }
}
