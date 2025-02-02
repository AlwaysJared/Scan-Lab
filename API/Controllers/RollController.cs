using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Libs.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RollController : ControllerBase
    {
        private readonly RollRepository _rollRepository;

        public RollController(RollRepository rollRepository)
        {
            _rollRepository = rollRepository;
        }
    }
}