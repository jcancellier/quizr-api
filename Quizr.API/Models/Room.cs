using Microsoft.AspNetCore.SignalR;
using Quizr.API.Constants.Room;
using Quizr.API.Hubs;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace Quizr.API.Models
{
    public class Room
    {
        private readonly IHubContext<QuizrHub, IQuizrClient> _hubContext;

        public string Id { get; set; }
        public int QuizId { get; set; }
        public List<string> Users { get; set; } = new List<string>();
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
            Timer tempTimer = (Timer)timer;

            // set time and phase for next quiz phase
            switch (Phase)
            {
                case RoomPhase.Lobby:
                    ChangeRoomTime(RoomTime.Prequestion);
                    ChangeRoomPhase(RoomPhase.Prequestion);
                    break;
                case RoomPhase.Prequestion:
                    ChangeRoomTime(RoomTime.Question);
                    ChangeRoomPhase(RoomPhase.Question);
                    break;
                case RoomPhase.Question:
                    ChangeRoomTime(RoomTime.Postquestion);
                    ChangeRoomPhase(RoomPhase.Postquestion);
                    break;
                case RoomPhase.Postquestion:
                    // Special case which either leads to new question or ends quiz
                    // Check if on last question
                    if (CurrentQuestionIndex == QuizrHub.Quizzes[QuizId].Questions.Count - 1)
                    {
                        // Send quiz results
                        ChangeRoomPhase(RoomPhase.Finished);
                        SendQuizResults();
                        tempTimer.Stop();
                        ClearUsers();
                        CurrentQuestionIndex = 0;
                    }
                    else
                    {
                        // Move to next question
                        CurrentQuestionIndex++;
                        SendNewQuestion();
                        //<insert code here to switch question>
                        ChangeRoomTime(RoomTime.Prequestion);
                        ChangeRoomPhase(RoomPhase.Prequestion);
                    }
                    break;
            }
        }

        public void ChangeRoomPhase(RoomPhase newPhase)
        {
            Phase = newPhase;

            // emit new room phase to client
            _hubContext.Clients.Group(Id).UpdateRoomPhase(Phase.ToString().ToLower());
        }

        public async void ChangeRoomTime(RoomTime newTime)
        {
            CurrentTime = (int)newTime;
            await _hubContext.Clients.Group(Id).UpdateRoomTimer(CurrentTime);
        }

        private void ClearUsers()
        {
            foreach(var user in Users)
            {
                _hubContext.Groups.RemoveFromGroupAsync(QuizrHub.quizrClients[user].ConnectionId, Id);
            }
            Users.Clear();
        }

        private void SendQuizResults()
        {
            // get all user's in room
            var users = new List<User>();
            foreach (var user in Users)
            {
                users.Add(QuizrHub.quizrClients[user]);
            }

            // Find top 3 users
            var sortedUsers = users.OrderByDescending((u) => u.Score);

            var top3Users = new List<User>();
            if (sortedUsers.Count() >= 3)
                top3Users = sortedUsers.Take(3).ToList();
            else
            {
                top3Users = sortedUsers.ToList();
            }

            object quizResults = new
            {
                topThreeUsers = top3Users
            };

            _hubContext.Clients.Groups(Id).ReceiveQuizResults(quizResults);
        }

        private void SendNewQuestion()
        {
            var question = QuizrHub.Quizzes[QuizId].Questions[CurrentQuestionIndex];

            object questionToSend = new
            {
                text = question.Text,
                answers = question.Answers,
                currentQuestionIndex = CurrentQuestionIndex+1,
                questionCount = QuizrHub.Quizzes[QuizId].Questions.Count
            };

            _hubContext.Clients.Group(Id).ReceiveNewQuestion(questionToSend);
        }
    }
}
