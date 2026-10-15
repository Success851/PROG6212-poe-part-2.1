using Microsoft.EntityFrameworkCore;
using RaceDayTestAPI.Models;

namespace RaceDayTestAPI.Data
{
    public class RaceDayTestDbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public RaceDayTestDbContext(
            DbContextOptions<RaceDayTestDbContext> options)
            : base(options)
        {
        }

        public DbSet<Organiser> Organisers { get; set; }

        public DbSet<Participant> Participants { get; set; }

        public DbSet<Event> Events { get; set; }

        public DbSet<Category> Categories { get; set; }

        public DbSet<Enrolment> Enrolments { get; set; }

        public DbSet<Result> Results { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            // Organiser → Events
            modelBuilder.Entity<Organiser>()
                .HasMany(o => o.Events)
                .WithOne(e => e.Organiser)
                .HasForeignKey(e => e.OrganiserID)
                .OnDelete(DeleteBehavior.Restrict);


            // Event → Categories
            modelBuilder.Entity<Event>()
                .HasMany(e => e.Categories)
                .WithOne(c => c.Event)
                .HasForeignKey(c => c.EventID)
                .OnDelete(DeleteBehavior.Cascade);


            // Participant → Enrolments
            modelBuilder.Entity<Participant>()
                .HasMany(p => p.Enrolments)
                .WithOne(e => e.Participant)
                .HasForeignKey(e => e.ParticipantID)
                .OnDelete(DeleteBehavior.Cascade);


            // Event → Enrolments
            // Restrict prevents multiple cascade paths in SQL Server
            modelBuilder.Entity<Event>()
                .HasMany(e => e.Enrolments)
                .WithOne(e => e.Event)
                .HasForeignKey(e => e.EventID)
                .OnDelete(DeleteBehavior.Restrict);


            // Category → Enrolments
            // Restrict prevents multiple cascade paths
            modelBuilder.Entity<Category>()
                .HasMany(c => c.Enrolments)
                .WithOne(e => e.Category)
                .HasForeignKey(e => e.CategoryID)
                .OnDelete(DeleteBehavior.Restrict);


            // Enrolment → Result
            modelBuilder.Entity<Enrolment>()
                .HasOne(e => e.Result)
                .WithOne(r => r.Enrolment)
                .HasForeignKey<Result>(r => r.EnrolmentID)
                .OnDelete(DeleteBehavior.Cascade);


            // Organiser email must be unique
            modelBuilder.Entity<Organiser>()
                .HasIndex(o => o.Email)
                .IsUnique();


            // Participant email must be unique
            modelBuilder.Entity<Participant>()
                .HasIndex(p => p.Email)
                .IsUnique();


            // Participant cannot enrol in the same event twice
            modelBuilder.Entity<Enrolment>()
                .HasIndex(e => new
                {
                    e.ParticipantID,
                    e.EventID
                })
                .IsUnique();


            // Category name must be unique within an event
            modelBuilder.Entity<Category>()
                .HasIndex(c => new
                {
                    c.EventID,
                    c.CategoryName
                })
                .IsUnique();
        }
    }
}
