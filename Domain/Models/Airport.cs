namespace Domain.Models
{
    public class Airport
    {
        public string Name { get; set; }
        public string Alias { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int Altitude { get; set; }
    }

}
