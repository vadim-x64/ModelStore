using Microsoft.EntityFrameworkCore;
using ModelStore.Models;

namespace ModelStore.Data
{
    public class DB : DbContext
    {
        public DB(DbContextOptions<DB> options) : base(options) { }
        public DbSet<Registration> Users { get; set; }
        public DbSet<Login> User { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
    }
}