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
        private static ConcurrentDictionary<string, User> QuizrClients = new ConcurrentDictionary<string, User>();

        public override Task OnConnectedAsync()
        {
            Console.WriteLine($"{Context.ConnectionId} connected");
            return base.OnConnectedAsync();
        }
    }
}
