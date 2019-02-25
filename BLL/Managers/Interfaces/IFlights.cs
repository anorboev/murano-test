using Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Managers.Interfaces
{
    public interface IFlights
    {
        List<Route> GetRoute(string src, string dest, out string msg);
    }
}
