using FlightsWrapper.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlightsWrapper;
using Xunit;
using Domain.Models;

namespace FlightsApiWrapper.Tests
{
    public class AirlineSearchTest
    {
        private readonly IFlightsApi _flightsApi;

        public AirlineSearchTest()
        {
            _flightsApi = new FlightsApi();
        }

        [Fact]
        public async Task ShouldFailForEmptyString()
        {
            var airline = await _flightsApi.GetAirline("");
            Assert.Null(airline);
        }

        [Fact]
        public async Task ShouldFailForShortString()
        {
            var airline = await _flightsApi.GetAirline("u");
            Assert.Null(airline);
        }

        [Fact]
        public async Task ShouldFailForInvalidString()
        {
            var airline = await _flightsApi.GetAirline("123");
            Assert.Empty(airline);
        }

        [Fact]
        public async Task ShouldSuccessForValidString()
        {
            var airline = await _flightsApi.GetAirline("7H");
            Assert.NotEmpty(airline);
        }
    }
}
