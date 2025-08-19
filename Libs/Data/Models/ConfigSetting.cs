using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Libs.Data.Models
{
    public class ConfigSetting
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public string? Key { get; set; }
        public string? Value { get; set; }
    }
}