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
        public int QuizId { get; set; }
        public int CurrentQuestionIndex { get; set; } = 0;
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
                ChangeRoomPhase(RoomPhase.Lobby);
            }
        }

        public async void UpdateTime(object source, ElapsedEventArgs e)
        {
            CurrentTime--;

            // Emit time change to Room
            await _hubContext.Clients.Group(Id).UpdateRoomTimer(CurrentTime);

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
            Timer tempTimer = (Timer)timer;

            // set time and phase for next quiz phase
            switch (Phase)
            {
                case RoomPhase.Lobby:
                    CurrentTime = (int)RoomTime.Prequestion;
                    ChangeRoomPhase(RoomPhase.Prequestion);
                    break;
                case RoomPhase.Prequestion:
                    CurrentTime = (int)RoomTime.Question;
                    ChangeRoomPhase(RoomPhase.Question);
                    break;
                case RoomPhase.Question:
                    CurrentTime = (int)RoomTime.Postquestion;
                    ChangeRoomPhase(RoomPhase.Postquestion);
                    break;
                case RoomPhase.Postquestion:
                    // Special case which either leads to new question or ends quiz
                    // Check if on last question
                    if (CurrentQuestionIndex == QuizrHub.Quizzes[QuizId].Questions.Count - 1)
                    {
                        //<insert code to emit quiz results>
                        ChangeRoomPhase(RoomPhase.Finished);
                        tempTimer.Stop();
                    }
                    else
                    {
                        // Move to next question
                        CurrentQuestionIndex++;
                        // <insert code for changing question on client here>
                        CurrentTime = (int)RoomTime.Prequestion;
                        ChangeRoomPhase(RoomPhase.Prequestion);
                    }
                    break;
            }
        }

        public async void ChangeRoomPhase(RoomPhase newPhase)
        {
            Phase = newPhase;

            // emit new room phase to client
            await _hubContext.Clients.Group(Id).UpdateRoomPhase(Phase.ToString().ToLower());
        }
    }
}
