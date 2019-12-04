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
        public static ConcurrentDictionary<string, User> quizrClients = new ConcurrentDictionary<string, User>();

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
                        Text = "What year was CSUB opened?",
                        Answers = new List<string>
                        {
                            "1971",
                            "1980",
                            "1965",
                            "1970"
                        },
                        CorrectAnswerIndex = 3
                    },
                    new Question
                    {
                        Text = "How many students were enrolled for Fall 2019 at CSUB?",
                        Answers = new List<string>
                        {
                            "10,158",
                            "11,206",
                            "14,344",
                            "9,878"
                        },
                        CorrectAnswerIndex = 1
                    },
                    new Question
                    {
                        Text = "From 1979-1981 which CSUB sports team was Gordon the captain of?",
                        Answers = new List<string>
                        {
                            "Water Polo",
                            "Golf",
                            "Tennis",
                            "Cheerleading"
                        },
                        CorrectAnswerIndex = 1
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
                quizrRooms[0].Id = "#1234";

                // Make room host first quiz available in the this.Quizzes list
                quizrRooms[0].QuizId = 0;
            }

            Console.WriteLine($"{Context.ConnectionId} connected");
            return base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            User userToDelete = quizrClients.FirstOrDefault(client => client.Value.ConnectionId == Context.ConnectionId).Value;
            bool userRemoved;

            // If user found
            if(userToDelete != null)
            {
                //Remove user from any groups he/she is connected to.
                //TODO: refactor user to contain roomId and room to not contain reference to all users.
                foreach (var room in quizrRooms)
                {
                    if (room.Users.Contains(userToDelete.Name))
                    {
                        room.Users.Remove(userToDelete.Name);
                        await Groups.RemoveFromGroupAsync(userToDelete.ConnectionId, room.Id);
                        await Clients.Group(room.Id).UpdateQuizRoomUsers(room.Users.Count);
                    }
                }

                // Remove user from connected quizrClients
                userRemoved = quizrClients.TryRemove(userToDelete.Name, out _);

                if (userRemoved)
                    Console.WriteLine("user removed");
                else
                    Console.WriteLine("Unable to remove user");
            }

            //return base.OnDisconnectedAsync(exception);
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

                //Add user to room
                quizrRooms.FirstOrDefault(r => r.Id == roomId).Users.Add(userName);

                await Clients.Group(room.Id).UpdateQuizRoomUsers(room.Users.Count);

                // send question to client
                await Clients.Client(quizrClients[userName].ConnectionId).ReceiveNewQuestion(Quizzes[room.QuizId].Questions[room.CurrentQuestionIndex]);

                // Send current time to user
                room.StartTimer();
                await Clients.Group(room.Id).UpdateRoomTimer(room.CurrentTime);
                return room;
            } 

            throw new HubException("Group Does not exist");
        }
    }
}
