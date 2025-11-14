using ABC_Retailers.Models;
using ABC_Retailers.Models.Login_Register;
using Microsoft.EntityFrameworkCore;

namespace ABC_Retailers.Data
{
    public class RetailersDbContext : DbContext
    {
        public RetailersDbContext(DbContextOptions<RetailersDbContext> options) : base(options) { }

        public DbSet<Users> Users => Set<Users>();

        public DbSet<Cart> Cart => Set<Cart>();
    }
}