using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using Libs.Data.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace Libs.Data.Context
{
    public class ScanLabContext 
        : IdentityDbContext<Staff, IdentityRole<Guid>, Guid>
    {
        public ScanLabContext(DbContextOptions<ScanLabContext> options) : base(options) { }

        public DbSet<Order> Orders { get; set; }
        public DbSet<Scanner> Scanners { get; set; }
        public DbSet<Roll> Rolls { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Staff> Staff { get; set; }
        public DbSet<ConfigSetting> ConfigSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Order>()
                .HasMany(o => o.Rolls)
                .WithOne(r => r.Order)
                .HasForeignKey(r => r.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Rename Identity tables to match "Staff"
            modelBuilder.Entity<Staff>().ToTable("Staff");
            modelBuilder.Entity<IdentityRole<Guid>>().ToTable("Roles");
            modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("StaffRoles");
            modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("StaffClaims");
            modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("StaffLogins");
            modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
            modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("StaffTokens");
        }
    }
}