using Microsoft.AspNetCore.Identity;

namespace BackgroundService.Models
{
    public class Player
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public IdentityUser User { get; set; }
        // TODO: Ajouter une propriété NbWins
        public int NbWins { get; set; }
    }
}
