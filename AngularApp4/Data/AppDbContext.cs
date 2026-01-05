using AngularApp4.Model;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace AngularApp4.Data
{
    
        public class AppDbContext : DbContext
        {
            public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

      
        public DbSet<Service> Services { get; set; }
        public DbSet<Appointment> Appointments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

         
            modelBuilder.Entity<Service>().HasData(
                new Service
                {
                    Id = 1,
                    Name = "Initial Consultation",
                    Category = "Health",
                    DurationMinutes = 45,
                    Price = 75.00m,
                    Icon = "medical_services",
                    Color = "#3b82f6"
                },
                new Service
                {
                    Id = 2,
                    Name = "Deep Tissue Massage",
                    Category = "Therapy",
                    DurationMinutes = 60,
                    Price = 120.00m,
                    Icon = "spa",
                    Color = "#8b5cf6"
                }
            );
        }
    }
 
}
