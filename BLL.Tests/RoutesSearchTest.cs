using BLL.Managers.Interfaces;
using FlightsWrapper;
using FlightsWrapper.Interfaces;
using System;
using BLL.Managers;
using Xunit;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;

namespace BLL.Tests
{
    public class RoutesSearchTest
    {
        private readonly IFlightsApi _flightsApi;
        private readonly IFlights _flights;
        private readonly ILogger<Flights> _logger;


        public RoutesSearchTest()
        {
            _logger = Mock.Of<ILogger<Flights>>();
            _flightsApi = new FlightsApi();
            _flights = new Flights(_flightsApi, _logger);
        }

        [Fact]
        public async Task ShouldFailForEmptySourceString()
        {
            var route = await _flights.GetRoute("", "RSH");
            Assert.False(route.IsRouteFound);
        }

        [Fact]
        public async Task ShouldFailForEmptyDestinationString()
        {
            var route = await _flights.GetRoute("RSH", "");
            Assert.False(route.IsRouteFound);
        }

        [Fact]
        public async Task ShouldFailForInvalidSourceString()
        {
            var route = await _flights.GetRoute("qwer7890", "RSH");
            Assert.False(route.IsRouteFound);
        }

        [Fact]
        public async Task ShouldFailForInvalidDestinationString()
        {
            var route = await _flights.GetRoute("RSH", "qwer7890");
            Assert.False(route.IsRouteFound);
        }

        [Fact]
        public async Task ShouldSuccessForValidDestinationString()
        {
            var route = await _flights.GetRoute("RSH", "ANV");
            Assert.True(route.IsRouteFound);
            Assert.NotEmpty(route.Routes);
        }
    }
}
