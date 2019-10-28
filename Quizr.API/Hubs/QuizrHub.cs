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
        /// Dictionary mapping connectionIds to Users
        /// </summary>
        private static ConcurrentDictionary<string, User> quizrClients = new ConcurrentDictionary<string, User>();
         
        /// <summary>
        /// Dictionary mapping room IDs to Rooms (represented by groups)
        /// </summary>
        private static ConcurrentDictionary<string, Room> quizrRooms = new ConcurrentDictionary<string, Room> {
             ["#test"] = new Room { Id = "#test" } 
        };

        public override Task OnConnectedAsync()
        {
            Console.WriteLine($"{Context.ConnectionId} connected");
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            User userToDelete = quizrClients.Where(client => client.Value.ConnectionId == Context.ConnectionId).FirstOrDefault().Value;
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
            bool roomExists = quizrRooms.ContainsKey(roomId);
            if (roomExists)
            {
                await Groups.AddToGroupAsync(quizrClients[userName].ConnectionId, roomId);
                await Clients.Group(roomId).SendAsync("GroupMessage", $"{userName} connected!!!");
                return quizrRooms[roomId];
            } 
            else
            {
                throw new HubException("Group Does not exist");
            }
        }
    }
}
