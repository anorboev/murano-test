using BLL.Models;
using Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Managers.Interfaces
{
    public interface IFlights
    {
        Task<RouteResultModel> GetRoute(string src, string dest);
    }
}
