using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Libs.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Libs.Data.Context
{
    public class ScanLab_LogContext : DbContext
    {
        public ScanLab_LogContext(DbContextOptions<ScanLab_LogContext> options)
            : base(options) { }

        public DbSet<LogEntry> Logs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<LogEntry>(entity =>
            {
                entity.ToTable("logs");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Message).HasColumnName("message");
                entity.Property(e => e.MessageTemplate).HasColumnName("message_template");
                entity.Property(e => e.Level).HasColumnName("level");
                entity.Property(e => e.Timestamp).HasColumnName("timestamp");
                entity.Property(e => e.Exception).HasColumnName("exception");
                entity.Property(e => e.Properties).HasColumnName("properties");
                entity.Property(e => e.LogEvent).HasColumnName("log_event");

                entity.Property(e => e.Area)
                    .HasColumnName("area")
                    .HasDefaultValue("System");
            });
        }
    }
}