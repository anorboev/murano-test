using FlightsWrapper.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlightsWrapper;
using Xunit;
using Domain.Models;

namespace FlightsApiWrapper.Tests
{
    public class AirportSearchTest
    {

        private readonly IFlightsApi _flightsApi;

        public AirportSearchTest()
        {
            _flightsApi = new FlightsApi();
        }

        [Fact]
        public async Task ShouldFailForEmptyString()
        {
            List<Airport> airports = new List<Airport>();
            airports = await _flightsApi.GetAirports("");
            Assert.Null(airports);
        }

        [Fact]
        public async Task ShouldFailForShortString()
        {
            List<Airport> airports = new List<Airport>();
            airports = await _flightsApi.GetAirports("uz");
            Assert.Null(airports);
        }

        [Fact]
        public async Task ShouldReturnEmptyCollectionForInvalidString()
        {
            List<Airport> airports = new List<Airport>();
            airports = await _flightsApi.GetAirports("za850as");
            Assert.Empty(airports);
        }

        [Fact]
        public async Task ShouldReturnListForValidString()
        {
            List<Airport> airports = new List<Airport>();
            airports = await _flightsApi.GetAirports("uzb");
            Assert.NotEmpty(airports);
        }
    }
}
