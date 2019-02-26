using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BLL.Managers.Interfaces;
using BLL.Models;
using Domain.Models;
using FlightsWrapper.Interfaces;

namespace BLL.Managers
{
    public class Flights: IFlights
    {
        private readonly IFlightsApi _flightsApi;
        public Flights(IFlightsApi flightsApi)
        {
            _flightsApi = flightsApi;
        }

        public async Task<RouteResultModel> GetRoute(string src, string dest)
        {
            var result = new RouteResultModel();
            var airportCheck = await CheckAirports(src, dest);
            if (!airportCheck.success)
            {
                result.IsRouteFound = false;
                result.Message = airportCheck.message;
                return result;
            }

            var airlines = new ConcurrentBag<Airline>();
            var routesList = new ConcurrentBag<RouteLevelModel>();
            (await GetRoutesAsync(src, airlines, new ConcurrentBag<RouteLevelModel>())).ForEach(route =>
            {
                routesList.Add(new RouteLevelModel
                {
                    Level = 1,
                    Route = route
                });
            });
            var destRoute = routesList.FirstOrDefault(x => x.Route.DestAirport == dest);

            int level = 1;
            if (destRoute == null)
            {
                while (true)
                {
                    var routes = routesList.Where(x => x.Level == level).ToList();

                    if(!routes.Any())
                        break;

                    level++;
                    List<RouteLevelModel> routeLevel = new List<RouteLevelModel>();

                    var task = Task.Run(() =>
                    {
                        Parallel.ForEach(routes,
                        new ParallelOptions { MaxDegreeOfParallelism = 5 },
                        (route, state) =>
                        {
                            if (state.IsStopped)
                                return;
                            var currRoutes = GetRoutesAsync(route.Route.DestAirport, airlines, routesList).Result;
                            routeLevel = currRoutes.Select(r => new RouteLevelModel()
                            {
                                Level = level,
                                Route = r,
                                Parent = route
                            }).ToList();

                            if (destRoute == null)
                                destRoute = routeLevel.FirstOrDefault(x => x.Route.DestAirport == dest);

                            if (destRoute != null)
                                state.Stop();

                            routeLevel.ForEach(x => routesList.Add(x));
                        });
                    });

                    await task;

                    if (destRoute != null)
                        break;
                }
            }

            if (destRoute != null)
            {
                for (int i = level; i > 0; i--)
                {
                    result.Routes.Add(destRoute.Route);
                    destRoute = destRoute.Parent;
                }
                result.Routes.Reverse();
                result.IsRouteFound = true;
            }

            return result;
        }


        private async Task<(bool success, string message)> CheckAirports(string src, string dest)
        {
            if (string.IsNullOrEmpty(src))
            {
                return (false, "Source airport name cannot be empty");
            }
            else if (string.IsNullOrEmpty(dest))
            {
                return (false, "Destination airport name cannot be empty");
            }

            var airports = await _flightsApi.GetAirports(src);
            if (!airports.Any(x => x.Alias == src))
            {
                return (false, "Source airport not found");
            }

            airports = await _flightsApi.GetAirports(dest);
            if (!airports.Any(x => x.Alias == dest))
            {
                return (false, "Destination airport not found");
            }

            return (true, "");
        }

        private async Task<bool> IsActiveAirlineAsync(string alias, ConcurrentBag<Airline> airlines)
        {
            var airline = airlines.FirstOrDefault(x => x.Alias == alias);
            if (airline != null)
                return airline.Active;
            airline = (await _flightsApi.GetAirline(alias))?.FirstOrDefault();
            if (airline != null)
            {
                airlines.Add(airline);
                return airline.Active;
            }
            return false;
        }

        private async Task<List<Route>> GetRoutesAsync(string airport, ConcurrentBag<Airline> airlines, ConcurrentBag<RouteLevelModel> routes)
        {
            List<Route> _routes = new List<Route>();
            var route = routes.FirstOrDefault(x => x.Route.SrcAirport == airport);
            if (route == null)
            {
                _routes = await _flightsApi.GetRoutes(airport);

                var taskList = _routes.Select(item => new { Item = item, PredTask = IsActiveAirlineAsync(item.Airline, airlines) }).ToList();
                await Task.WhenAll(taskList.Select(x => x.PredTask));
                _routes = taskList.Where(x => x.PredTask.Result).Select(x => x.Item).ToList();
            } 
            return _routes;
        }
    }
}
