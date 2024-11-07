using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Libs.Enumerables;

namespace Libs.Data.Models
{
    public class Rolls
    {
        public Guid RollId { get; set; }
        public long RollNumber { get; set; }
        public int ImageCount { get; set; }
        public FilmType FilmType{ get; set; }
    }
}