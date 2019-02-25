using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Domain.Models;
using FlightsWrapper.Interfaces;
using RestSharp;

namespace FlightsWrapper
{
    public class FlightsApi: IFlightsApi
    {
        const string BaseUrl = "https://homework.appulate.com/";

        public Task<List<Airport>> GetAirports(string alias)
        {
            var url = BaseUrl + "api/Airport/search?pattern=" + alias;
            var client = new RestClient(url);
            return ExecuteAsync<List<Airport>>(client);
        }

        public Task<List<Route>> GetRoutes(string alias)
        {
            var url = BaseUrl + "api/Route/outgoing?airport=" + alias;
            var client = new RestClient(url);
            return ExecuteAsync<List<Route>>(client);
        }

        public Task<Airline> GetAirline(string alias)
        {
            var url = BaseUrl + "api/Airline/" + alias;
            var client = new RestClient(url);
            return ExecuteAsync<Airline>(client);
        }

        private Task<T> ExecuteAsync<T>(RestClient client) where T : new()
        {
            var request = new RestRequest();
            var taskCompletionSource = new TaskCompletionSource<T>();
            client.ExecuteAsync<T>(request, (response) => taskCompletionSource.SetResult(response.Data));
            return taskCompletionSource.Task;
        }
    }
}
