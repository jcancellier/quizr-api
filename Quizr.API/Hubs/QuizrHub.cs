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

        public override Task OnConnectedAsync()
        {
            Console.WriteLine($"{Context.ConnectionId} connected");
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            User userToDelete = quizrClients.Where(client => client.Value.ConnectionId == Context.ConnectionId).FirstOrDefault().Value;
            bool userRemoved;

            if(userToDelete != null)
            {
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
    }
}
