using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using Libs.Data.Models;

namespace Libs.Data.Context
{
    public class ScanLabContext : DbContext
    {
        public ScanLabContext(DbContextOptions<ScanLabContext> options) : base(options) { }

        public DbSet<Order> Orders { get; set; }
        public DbSet<Scanner> Scanners { get; set; }
        public DbSet<Roll> Rolls { get; set; }
        public DbSet<Customer> Customers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Order>()
                .HasMany(o => o.Rolls)
                .WithOne(r => r.Order)
                .HasForeignKey(r => r.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}