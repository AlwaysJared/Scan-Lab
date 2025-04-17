using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Libs.Enums;
using Microsoft.EntityFrameworkCore;

namespace Libs.Data.Models
{
    [Index(nameof(RollNumber), IsUnique = true)]
    public class Roll
    {
        [Key]
        public Guid RollId { get; set; }
        public long RollNumber { get; set; }
        public int? ImageCount { get; set; }
        public FilmType? FilmType{ get; set; }
        public List<string>? RollNotes { get; set; }
        public string? OrderId { get; set; }
        public Order? Order { get; set; }
        public RollStatus Status { get; set; } = RollStatus.Created;
        public DateTime? DateCreated { get; set; } = DateTime.Now;
        public DateTime? DateUpdated { get; set; }
    }
}