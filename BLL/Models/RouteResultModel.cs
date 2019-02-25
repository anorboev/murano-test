using Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.Models
{
    public class RouteResultModel
    {
        public List<Route> Routes { get; set; }
        public bool IsRouteFound { get; set; }
        public string Message { get; set; }
    }
}
