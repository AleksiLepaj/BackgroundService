using BackgroundService.Data;
using BackgroundService.DTOs;
using BackgroundService.Models;
using BackgroundService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BackgroundService.Hubs
{
    [Authorize]
    public class GameHub : Hub
    {
        private Game _game;
        private BackgroundServiceContext _backgroundServiceContext;

        public GameHub(Game game, BackgroundServiceContext backgroundServiceContext)
        {
            _game = game;
            _backgroundServiceContext = backgroundServiceContext;
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
            _game.AddUser(Context.UserIdentifier!);

            Player player = _backgroundServiceContext.Player.Where(p => p.UserId == Context.UserIdentifier!).Single();

            await Clients.Caller.SendAsync("GameInfo", new GameInfoDTO()
            {
                NbWins = player.NbWins,
                MultiplierCost = Game.MULTIPLIER_BASE_PRICE
            });
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _game.RemoveUser(Context.UserIdentifier!);
            await base.OnDisconnectedAsync(exception);
        }

        public void Increment()
        {
            _game.Increment(Context.UserIdentifier!);
        }

        public void BuyMultiplier()
        {
            _game.BuyMultiplier(Context.UserIdentifier!);
        }
    }
}
