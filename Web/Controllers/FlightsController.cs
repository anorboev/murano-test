using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BLL.Managers.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class FlightsController : ControllerBase
    {
        private IFlights _flights;

        public FlightsController(IFlights flights)
        {
            _flights = flights;
        }

        // GET api/values
        [HttpGet]
        public async Task<ActionResult> GetRoutes(string src, string dest)
        {
            var flights = await _flights.GetRoute(src, dest);

            if (flights.IsRouteFound)
                return Ok(flights.Routes);

            return Ok(flights.Message);
        }
    }
}