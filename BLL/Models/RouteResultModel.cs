using Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.Models
{
    public class RouteResultModel
    {
        public RouteResultModel()
        {
            Routes = new List<Route>();
        }
        public List<Route> Routes { get; set; }
        public bool IsRouteFound { get; set; }
        public string Message { get; set; }
    }
}
