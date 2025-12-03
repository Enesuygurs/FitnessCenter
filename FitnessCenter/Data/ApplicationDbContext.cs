using FitnessCenter.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Gym> Gyms { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<Trainer> Trainers { get; set; }
        public DbSet<TrainerService> TrainerServices { get; set; }
        public DbSet<TrainerAvailability> TrainerAvailabilities { get; set; }
        public DbSet<Appointment> Appointments { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Gym configuration
            builder.Entity<Gym>(entity =>
            {
                entity.HasKey(g => g.Id);
                entity.Property(g => g.Name).IsRequired().HasMaxLength(100);
                entity.Property(g => g.Address).IsRequired().HasMaxLength(500);
                entity.Property(g => g.Phone).IsRequired().HasMaxLength(20);
            });

            // Service configuration
            builder.Entity<Service>(entity =>
            {
                entity.HasKey(s => s.Id);
                entity.Property(s => s.Name).IsRequired().HasMaxLength(100);
                entity.Property(s => s.Price).HasColumnType("decimal(18,2)");
                entity.HasOne(s => s.Gym)
                      .WithMany(g => g.Services)
                      .HasForeignKey(s => s.GymId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Trainer configuration
            builder.Entity<Trainer>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.FirstName).IsRequired().HasMaxLength(50);
                entity.Property(t => t.LastName).IsRequired().HasMaxLength(50);
                entity.Property(t => t.Email).IsRequired().HasMaxLength(100);
                entity.HasOne(t => t.Gym)
                      .WithMany(g => g.Trainers)
                      .HasForeignKey(t => t.GymId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // TrainerService (Many-to-Many) configuration
            builder.Entity<TrainerService>(entity =>
            {
                entity.HasKey(ts => ts.Id);
                entity.HasOne(ts => ts.Trainer)
                      .WithMany(t => t.TrainerServices)
                      .HasForeignKey(ts => ts.TrainerId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(ts => ts.Service)
                      .WithMany(s => s.TrainerServices)
                      .HasForeignKey(ts => ts.ServiceId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // TrainerAvailability configuration
            builder.Entity<TrainerAvailability>(entity =>
            {
                entity.HasKey(ta => ta.Id);
                entity.HasOne(ta => ta.Trainer)
                      .WithMany(t => t.Availabilities)
                      .HasForeignKey(ta => ta.TrainerId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Appointment configuration
            builder.Entity<Appointment>(entity =>
            {
                entity.HasKey(a => a.Id);
                entity.Property(a => a.TotalPrice).HasColumnType("decimal(18,2)");
                entity.HasOne(a => a.User)
                      .WithMany(u => u.Appointments)
                      .HasForeignKey(a => a.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(a => a.Trainer)
                      .WithMany(t => t.Appointments)
                      .HasForeignKey(a => a.TrainerId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(a => a.Service)
                      .WithMany(s => s.Appointments)
                      .HasForeignKey(a => a.ServiceId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
