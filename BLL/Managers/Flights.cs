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

        //public async Task<RouteResultModel> GetRoute(string src, string dest)
        //{
        //    var result = new RouteResultModel();
        //    var airportCheck = await CheckAirports(src, dest);
        //    if (!airportCheck.success)
        //    {
        //        result.IsRouteFound = false;
        //        result.Message = airportCheck.message;
        //        return result;
        //    }

        //    List<Airline> airlines = new List<Airline>();
        //    List<Route> routes = new List<Route>();

        //    result.Routes = await GetRoutes(src, dest, airlines, routes);
        //    result.IsRouteFound = result.Routes.Any(x => x.DestAirport == dest);

        //    return result;
        //}

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

            var airlines = new List<Airline>();
            ConcurrentBag<RouteLevelModel> routesList = new ConcurrentBag<RouteLevelModel>();
            (await GetRoutesAsync(src, airlines, new List<Route>())).ForEach(route =>
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
                    var level1 = level;
                    List<RouteLevelModel> routeLevel = new List<RouteLevelModel>();
                    Parallel.ForEach(routes, 
                        new ParallelOptions {MaxDegreeOfParallelism = 5},
                        async (route, state) =>
                        {
                            var level2 = level1;
                            routeLevel = (await GetRoutesAsync(route.Route.DestAirport, airlines, new List<Route>())).Select(r => new RouteLevelModel()
                            {
                                Level = level2,
                                Route = r,
                                Parent = route
                            }).ToList();

                            destRoute = routeLevel.FirstOrDefault(x => x.Route.DestAirport == dest);
                            if (destRoute != null)
                                state.Break();

                            routeLevel.ForEach(x => routesList.Add(x));
                        });

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


        //private async Task<List<Route>> GetRoutes(string src, string dest, List<Airline> airlines, List<Route> routes)
        //{
        //    var result = new List<Route>();
        //    var resRoutes = await GetRoutes1111111111111(src, airlines, routes);
        //    if (!resRoutes.Any())
        //        return result;

        //    var destRoute = resRoutes.FirstOrDefault(x => x.DestAirport == dest);
        //    if (destRoute != null)
        //    {
        //        result.Add(destRoute);
        //        return result;
        //    }

        //    Task[] tasks = new Task[resRoutes.Count];
        //    for (int i = 0; i < resRoutes.Count; i++)
        //    {
        //        tasks[i] = GetRoutes(resRoutes[i].DestAirport, dest, airlines, routes);
        //    }

        //    Parallel.ForEach(resRoutes, new ParallelOptions {MaxDegreeOfParallelism = 5},
        //        (route) => { route.ToString(); });

        //    return result;











        //foreach (var route in resRoutes)
        //{
        //    var res = await GetRoutes(route.DestAirport, dest, airlines, routes);
        //    if (res.Count > 0)
        //    {
        //        result.AddRange(res);
        //        result.Add(route);
        //        break;
        //    }
        //}
        //}

        private async Task<bool> IsActiveAirlineAsync(string alias, List<Airline> airlines)
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

        private async Task<List<Route>> GetRoutesAsync(string airport, List<Airline> airlines, List<Route> routes)
        {
            List<Route> _routes = new List<Route>();
            var route = routes.FirstOrDefault(x => x.SrcAirport == airport);
            if (route == null)
            {
                _routes = await _flightsApi.GetRoutes(airport);

                var taskList = _routes.Select(item => new { Item = item, PredTask = IsActiveAirlineAsync(item.Airline, airlines) }).ToList();
                await Task.WhenAll(taskList.Select(x => x.PredTask));
                _routes = taskList.Where(x => x.PredTask.Result).Select(x => x.Item).ToList();

                routes.AddRange(_routes);
            } 
            return _routes;
        }
    }
}
