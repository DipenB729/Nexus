using AngularApp4.Model;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace AngularApp4.Data
{
    
        public class AppDbContext : DbContext
        {
            public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

            // This line tells EF Core to create an 'Employees' table based on your Employee model
            public DbSet<Employee> Employees { get; set; }
        }
 
}
