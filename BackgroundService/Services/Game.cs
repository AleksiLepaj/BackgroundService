﻿using BackgroundService.Data;
using BackgroundService.Hubs;
using BackgroundService.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SuperChance.DTOs;

namespace BackgroundService.Services
{
    public class Game : Microsoft.Extensions.Hosting.BackgroundService
    {
        public const int DELAY = 30 * 1000;

        private Dictionary<string, int> _data = new();

        private IHubContext<GameHub> _gameHub;

        private IServiceScopeFactory _serviceScopeFactory;

        public Game(IHubContext<GameHub> gameHub, IServiceScopeFactory serviceScopeFactory)
        {
            _gameHub = gameHub;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public void AddUser(string userId)
        {
            _data[userId] = 0;
        }

        public void RemoveUser(string userId)
        {
            _data.Remove(userId);
        }

        public void Increment(string userId)
        {
            _data[userId]++;
        }

        public async Task EndRound(CancellationToken stoppingToken)
        {
            List<string> winners = new List<string>();
            int biggestValue = 0;
            // Reset des compteurs
            foreach (var key in _data.Keys)
            {
                int value = _data[key];
                if (value > 0 && value >= biggestValue)
                {
                    if(value > biggestValue)
                    {
                        if (value > biggestValue)
                        {
                            winners.Clear();
                            biggestValue = value;
                        }
                        winners.Add(key);
                    }
                }
            }

            // Aucune participation!
            if(biggestValue == 0)
            {
                RoundResult noResult = new RoundResult()
                {
                    Winners = null,
                    NbClicks = 0
                };
                await _gameHub.Clients.All.SendAsync("EndRound", noResult, stoppingToken);
                return;
            }

            using (IServiceScope scope = _serviceScopeFactory.CreateScope())
            {
                BackgroundServiceContext backgroundServiceContext =
                    scope.ServiceProvider.GetRequiredService<BackgroundServiceContext>();

                List<Player> players = await backgroundServiceContext.Player.Where(p => winners.Contains(p.UserId)).ToListAsync();
                foreach (var player in players)
                {
                    player.NbWins++;
                }
                await backgroundServiceContext.SaveChangesAsync();

                List<IdentityUser> users = await backgroundServiceContext.Users.Where(u => winners.Contains(u.Id)).ToListAsync();

                RoundResult roundResult = new RoundResult()
                {
                    Winners = users.Select(p => p.UserName)!,
                    NbClicks = biggestValue
                };
                await _gameHub.Clients.All.SendAsync("EndRound", roundResult, stoppingToken);
            }
            

            // Reset des compteurs
            foreach(var key in _data.Keys)
            {
                _data[key] = 0;
            }
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await EndRound(stoppingToken);

                await Task.Delay(DELAY, stoppingToken);
            }
        }
    }
}