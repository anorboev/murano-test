using Domain.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlightsWrapper.Interfaces
{
    public interface IFlightsApi
    {
        Task<List<Airport>> GetAirports(string alias);
        Task<List<Route>> GetRoutes(string alias);
        Task<List<Airline>> GetAirline(string alias);
    }
}
