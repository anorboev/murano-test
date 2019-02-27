using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BLL.Managers.Interfaces;
using BLL.Models;
using Domain.Models;
using FlightsWrapper.Interfaces;
using Microsoft.Extensions.Logging;

namespace BLL.Managers
{
    public class Flights: IFlights
    {
        private readonly IFlightsApi _flightsApi;
        private readonly ILogger<Flights> _logger;
        public Flights(IFlightsApi flightsApi, ILogger<Flights> logger)
        {
            _flightsApi = flightsApi;
            _logger = logger;
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
                    List<RouteLevelModel> routeLevel;

                    var task = Task.Run(() =>
                    {
                        Parallel.ForEach(routes,
                        new ParallelOptions { MaxDegreeOfParallelism = 5 },
                        (route, state) =>
                        {
                            if (state.IsStopped)
                                return;
                            var currRoutes = GetRoutesAsync(route.Route.DestAirport, airlines, routesList).Result;
                            var level1 = level;
                            routeLevel = currRoutes.Select(r => new RouteLevelModel()
                            {
                                Level = level1,
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
            else
            {
                result.IsRouteFound = false;
                result.Message = "Route not found";
            }

            return result;
        }


        private async Task<(bool success, string message)> CheckAirports(string src, string dest)
        {
            if (string.IsNullOrEmpty(src))
            {
                return (false, "Source airport name cannot be empty");
            }

            if (string.IsNullOrEmpty(dest))
            {
                return (false, "Destination airport name cannot be empty");
            }

            var airports = await Retry(_flightsApi.GetAirports, src);
            if (airports.All(x => x.Alias != src))
            {
                return (false, "Source airport not found");
            }

            airports = await Retry(_flightsApi.GetAirports, dest);
            if (airports.All(x => x.Alias != dest))
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
            airline = (await Retry(_flightsApi.GetAirline, alias))?.FirstOrDefault();
            if (airline != null)
            {
                airlines.Add(airline);
                return airline.Active;
            }
            return false;
        }

        private async Task<List<Route>> GetRoutesAsync(string airport, ConcurrentBag<Airline> airlines, ConcurrentBag<RouteLevelModel> routes)
        {
            List<Route> resRoutes = new List<Route>();
            var route = routes.FirstOrDefault(x => x.Route.SrcAirport == airport);
            if (route == null)
            {
                resRoutes = await Retry(_flightsApi.GetRoutes, airport);

                var taskList = resRoutes.Select(item => new { Item = item, PredTask = IsActiveAirlineAsync(item.Airline, airlines) }).ToList();
                await Task.WhenAll(taskList.Select(x => x.PredTask));
                resRoutes = taskList.Where(x => x.PredTask.Result).Select(x => x.Item).ToList();
            } 
            return resRoutes;
        }

        private async Task<T> Retry<T>(Func<string, Task<T>> func, string alias, int retryCount = 3)
        {
            while (true)
            {
                try
                {
                    var result = await func(alias);
                    return result;
                }
                catch(Exception ex) 
                when (retryCount-- > 0)
                {
                    _logger.LogError($"{DateTime.Now} - MethodName: {func.Method.Name}, ParametrValue: {alias}, Exception Message: {ex.Message}");
                }
            }
        }
    }
}
