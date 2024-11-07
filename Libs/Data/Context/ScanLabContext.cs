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
    }
}