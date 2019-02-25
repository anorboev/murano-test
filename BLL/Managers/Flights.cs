using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BLL.Managers.Interfaces;
using Domain.Models;
using FlightsWrapper.Interfaces;

namespace BLL.Managers
{
    public class Flights: IFlights
    {
        private IFlightsApi _flightsApi;
        public Flights(IFlightsApi flightsApi)
        {
            _flightsApi = flightsApi;
        }

        public List<Route> GetRoute(string src, string dest, out string msg)
        {
            msg = "";
            List<Airline> airlines = new List<Airline>();
            List< Route > routes = new List<Route>();
            return  GetRoutes(src, dest, airlines, routes).Result;
        }

        private async Task<List<Route>> GetRoutes(string src, string dest, List<Airline> airlines, List<Route> routes)
        {
            var result = new List<Route>();
            var resRoutes = await GetRoutes(src, airlines, routes);
            if (!resRoutes.Any())
                return result;

            var destRoute = resRoutes.FirstOrDefault(x => x.DestAirport == dest);
            if (destRoute != null)
            {
                result.Add(destRoute);
                return result;
            }

            foreach (var route in resRoutes)
            {
                var res = await GetRoutes(route.DestAirport, dest, airlines, routes);
                if (res.Count > 0)
                {
                    result.AddRange(res);
                    result.Add(route);
                    break;
                }
            }
            return result;
        }

        private async Task<bool> IsActiveAirline(string alias, List<Airline> airlines)
        {
            var airline = airlines.FirstOrDefault(x => x.Alias == alias);
            if (airline != null)
                return airline.Active;
            airline = await _flightsApi.GetAirline(alias);
            if (airline != null)
            {
                airlines.Add(airline);
                return airline.Active;
            }
            return false;
        }

        private async Task<List<Route>> GetRoutes(string airport, List<Airline> airlines, List<Route> routes)
        {
            List<Route> _routes = new List<Route>();
            var route = routes.FirstOrDefault(x => x.SrcAirport == airport);
            if (route == null)
            {
                _routes = await _flightsApi.GetRoutes(airport);

                var taskList = _routes.Select(item => new { Item = item, PredTask = IsActiveAirline(item.Airline, airlines) }).ToList();
                await Task.WhenAll(taskList.Select(x => x.PredTask));
                _routes = taskList.Where(x => x.PredTask.Result).Select(x => x.Item).ToList();

                routes.AddRange(_routes);
            } 
            return _routes;
        }
    }
}
