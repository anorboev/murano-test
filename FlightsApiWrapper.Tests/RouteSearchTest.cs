using FlightsWrapper.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlightsWrapper;
using Xunit;
using Domain.Models;

namespace FlightsApiWrapper.Tests
{
    public class RouteSearchTest
    {
        private readonly IFlightsApi _flightsApi;

        public RouteSearchTest()
        {
            _flightsApi = new FlightsApi();
        }

        [Fact]
        public async Task ShouldFailForEmptyString()
        {
            var routes = await _flightsApi.GetRoutes("");
            Assert.Empty(routes);
        }

        [Fact]
        public async Task ShouldFailForShortString()
        {
            var routes = await _flightsApi.GetRoutes("uz");
            Assert.Empty(routes);
        }

        [Fact]
        public async Task ShouldFailForLongString()
        {
            var routes = await _flightsApi.GetRoutes("uzbekistan");
            Assert.Empty(routes);
        }

        [Fact]
        public async Task ShouldReturnEmptyCollectionForInvalidString()
        {
            var routes = await _flightsApi.GetRoutes("1234");
            Assert.Empty(routes);
        }

        [Fact]
        public async Task ShouldReturnListForValidString()
        {
            var routes = await _flightsApi.GetRoutes("RSH");
            Assert.NotEmpty(routes);
        }
    }
}
