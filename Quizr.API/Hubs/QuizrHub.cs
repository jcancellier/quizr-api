using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Quizr.API.Models;

namespace Quizr.API.Hubs
{
    public class QuizrHub : Hub
    {
        /// <summary>
        /// Dictionary mapping usernames to User objects (ensures unique usernames)
        /// </summary>
        private static ConcurrentDictionary<string, User> quizrClients = new ConcurrentDictionary<string, User>();
         
        /// <summary>
        /// Quiz Rooms available (represented by groups)
        /// </summary>
        private static List<Room> quizrRooms = new List<Room> {
             new Room { Id = "#test" } 
        };

        public override Task OnConnectedAsync()
        {
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
            if (room != null)
            {
                await Groups.AddToGroupAsync(quizrClients[userName].ConnectionId, roomId);
                return room;
            } 

            throw new HubException("Group Does not exist");
        }
    }
}
