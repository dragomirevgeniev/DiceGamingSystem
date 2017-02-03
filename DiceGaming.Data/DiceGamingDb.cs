using DiceGaming.Data.Entities;
using System.Data.Entity;

namespace DiceGaming.Data
{
    public class DiceGamingDb : DbContext
    {
        public DbSet<Game> Games { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Login> Logins { get; set; }
    }
}
