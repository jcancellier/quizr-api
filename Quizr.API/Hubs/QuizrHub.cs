using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Quizr.API.Models;
using Microsoft.AspNetCore.Http;
using Quizr.API.Services;
using Quizr.API.Constants.Room;

namespace Quizr.API.Hubs
{
    public class QuizrHub : Hub<IQuizrClient>
    {
        /// <summary>
        /// Dictionary mapping usernames to User objects (ensures unique usernames)
        /// </summary>
        private static ConcurrentDictionary<string, User> quizrClients = new ConcurrentDictionary<string, User>();

        /// <summary>
        /// Quiz Rooms available (represented by groups)
        /// </summary>
        private static List<Room> quizrRooms = new List<Room> {
             //new Room { Id = "#test" }
        };

        /// <summary>
        /// Quizzes available for rooms
        /// </summary>
        public static List<Quiz> Quizzes = new List<Quiz>
        {
            new Quiz
            {
                Name = "Test Quiz",
                Questions = new List<Question>
                {
                    new Question
                    {
                        Text = "Who is the current president of CSUB?",
                        Answers = new List<string>
                        {
                            "Lynnette Zelezny",
                            "Truett S. Cathy",
                            "Horace Mitchell",
                            "Vernon B. Harper Jr."
                        },
                        CorrectAnswerIndex = 0
                    },
                    new Question
                    {
                        Text = "Who is the current president of CSUB?",
                        Answers = new List<string>
                        {
                            "Lynnette Zelezny",
                            "Truett S. Cathy",
                            "Horace Mitchell",
                            "Vernon B. Harper Jr."
                        },
                        CorrectAnswerIndex = 0
                    }
                }
            }
        };

        public override Task OnConnectedAsync()
        {
            // Add default room for testing purposes when first user connects
            if (!quizrRooms.Any())
            {
                var quizrHubContextService = (IQuizrHubContextService)Context.GetHttpContext().RequestServices.GetService(typeof(IQuizrHubContextService));
                quizrRooms.Add(new Room(quizrHubContextService.GetQuizrHubContext()));
                quizrRooms[0].Id = "#test";

                // Make room host first quiz available in this.Quizzes list
                quizrRooms[0].QuizId = 0;
            }

            Console.WriteLine($"{Context.ConnectionId} connected");
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            User userToDelete = quizrClients.FirstOrDefault(client => client.Value.ConnectionId == Context.ConnectionId).Value;
            bool userRemoved;

            // If user found
            if(userToDelete != null)
            {
                // Remove user from connected quizrClients
                userRemoved = quizrClients.TryRemove(userToDelete.Name, out _);
                if (userRemoved)
                    Console.WriteLine("user removed");
                else
                    Console.WriteLine("Unable to remove user");
            }

            return base.OnDisconnectedAsync(exception);
        }

        public User Login(string name)
        {
            // If new user
            if (!quizrClients.ContainsKey(name))
            {
                User newUser = new User { Name = name, ConnectionId = Context.ConnectionId };
                bool isAddUserSuccessful = quizrClients.TryAdd(name, newUser);
                if (!isAddUserSuccessful)
                    throw new Exception("Unable to add user");

                return newUser;
            }

            throw new HubException("User already exists");
        }

        public async Task<Room> AddUserToRoom(string userName, string roomId)
        {
            Room room = quizrRooms.FirstOrDefault(r => r.Id == roomId);
            if (room != null && (room.Phase == RoomPhase.NotStarted || room.Phase == RoomPhase.Lobby || room.Phase == RoomPhase.Finished))
            {

                await Groups.AddToGroupAsync(quizrClients[userName].ConnectionId, roomId);

                // Send current time to user
                room.StartTimer();
                await Clients.Group(room.Id).UpdateRoomTimer(room.CurrentTime);
                return room;
            } 

            throw new HubException("Group Does not exist");
        }
    }
}
