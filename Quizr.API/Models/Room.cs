using Microsoft.AspNetCore.SignalR;
using Quizr.API.Constants.Room;
using Quizr.API.Hubs;
using System.Timers;

namespace Quizr.API.Models
{
    public class Room
    {
        private readonly IHubContext<QuizrHub, IQuizrClient> _hubContext;

        public string Id { get; set; }
        public RoomPhase Phase { get; set; } = RoomPhase.NotStarted;
        public int CurrentTime { get; set; } = (int) RoomTime.Lobby;
        public Timer Timer { get; set; } = new Timer();

        public Room(IHubContext<QuizrHub, IQuizrClient> quizrHubContext)
        {
            _hubContext = quizrHubContext;

            // Set up timer
            // trigger time change every second
            Timer.Interval = 1000;

            // Set timer event handler
            Timer.Elapsed += UpdateTime;

            // allow multiple event triggers
            Timer.AutoReset = true;
        }

        public void StartTimer()
        {
            if (Phase == RoomPhase.NotStarted || Phase == RoomPhase.Finished)
            {
                CurrentTime = (int)RoomTime.Lobby;
                Timer.Enabled = true;
                Phase = RoomPhase.Lobby;
            }
        }

        public void UpdateTime(object source, ElapsedEventArgs e)
        {
            CurrentTime--;

            // Emit time change to Room
            _hubContext.Clients.Group(Id).UpdateRoomTimer(CurrentTime);

            if (CurrentTime <= 0)
            {
                OnTimerFinished(source);
            }
        }

        public void OnTimerFinished(object timer)
        {
            // TODO: Quiz Phasing
            // 1. Lobby
            // 2. Prequestion
            // 3. Question
            // 4. Finished

            Phase = RoomPhase.Finished;

            // Stop Timer
            Timer tempTimer = (Timer)timer;
            tempTimer.Stop();
        }
    }
}
