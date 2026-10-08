using AcxiomCRM.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Lead> Leads => Set<Lead>();
        public DbSet<Opportunity> Opportunities => Set<Opportunity>();
        public DbSet<FollowUp> FollowUps => Set<FollowUp>();
        public DbSet<Activity> Activities => Set<Activity>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Customer indexes and configuration
            builder.Entity<Customer>(entity =>
            {
                entity.HasIndex(c => c.CustomerCode).IsUnique();
                entity.HasIndex(c => c.Email).IsUnique();
                entity.HasIndex(c => c.Phone).IsUnique();
                entity.HasIndex(c => c.Status);
                entity.HasIndex(c => c.AssignedToId);

                entity.HasOne(c => c.AssignedTo)
                    .WithMany(u => u.AssignedCustomers)
                    .HasForeignKey(c => c.AssignedToId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Lead indexes and configuration
            builder.Entity<Lead>(entity =>
            {
                entity.HasIndex(l => l.LeadCode).IsUnique();
                entity.HasIndex(l => l.Email);
                entity.HasIndex(l => l.Phone);
                entity.HasIndex(l => l.Status);
                entity.HasIndex(l => l.AssignedToId);
                entity.HasIndex(l => l.CustomerId);

                entity.HasOne(l => l.AssignedTo)
                    .WithMany(u => u.AssignedLeads)
                    .HasForeignKey(l => l.AssignedToId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(l => l.Customer)
                    .WithMany(c => c.Leads)
                    .HasForeignKey(l => l.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Opportunity indexes and configuration
            builder.Entity<Opportunity>(entity =>
            {
                entity.HasIndex(o => o.OpportunityCode).IsUnique();
                entity.HasIndex(o => o.Stage);
                entity.HasIndex(o => o.Status);
                entity.HasIndex(o => o.CustomerId);
                entity.HasIndex(o => o.LeadId);
                entity.HasIndex(o => o.AssignedToId);

                entity.HasOne(o => o.Customer)
                    .WithMany(c => c.Opportunities)
                    .HasForeignKey(o => o.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(o => o.Lead)
                    .WithMany(l => l.Opportunities)
                    .HasForeignKey(o => o.LeadId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(o => o.AssignedTo)
                    .WithMany(u => u.AssignedOpportunities)
                    .HasForeignKey(o => o.AssignedToId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // FollowUp configuration
            builder.Entity<FollowUp>(entity =>
            {
                entity.HasIndex(f => f.FollowUpDate);
                entity.HasIndex(f => f.Status);
                entity.HasIndex(f => f.AssignedToId);
                entity.HasIndex(f => f.CustomerId);
                entity.HasIndex(f => f.LeadId);

                entity.HasOne(f => f.Customer)
                    .WithMany(c => c.FollowUps)
                    .HasForeignKey(f => f.CustomerId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(f => f.Lead)
                    .WithMany(l => l.FollowUps)
                    .HasForeignKey(f => f.LeadId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(f => f.AssignedTo)
                    .WithMany(u => u.AssignedFollowUps)
                    .HasForeignKey(f => f.AssignedToId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Activity configuration
            builder.Entity<Activity>(entity =>
            {
                entity.HasIndex(a => a.ActivityDate);
                entity.HasIndex(a => a.ActivityType);
                entity.HasIndex(a => a.AssignedToId);
                entity.HasIndex(a => a.CustomerId);
                entity.HasIndex(a => a.LeadId);

                entity.HasOne(a => a.Customer)
                    .WithMany(c => c.Activities)
                    .HasForeignKey(a => a.CustomerId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(a => a.Lead)
                    .WithMany(l => l.Activities)
                    .HasForeignKey(a => a.LeadId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(a => a.AssignedTo)
                    .WithMany(u => u.AssignedActivities)
                    .HasForeignKey(a => a.AssignedToId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // AuditLog configuration
            builder.Entity<AuditLog>(entity =>
            {
                entity.HasIndex(a => a.CreatedDate);
                entity.HasIndex(a => a.UserId);
                entity.HasIndex(a => a.EntityName);
                entity.HasIndex(a => a.Action);

                entity.HasOne(a => a.User)
                    .WithMany(u => u.AuditLogs)
                    .HasForeignKey(a => a.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}
