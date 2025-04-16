namespace Library
{
    public class WildfireRecord
    {
        public string StreetNumber { get; set; }
        public string StreetName { get; set; }
        public string StreetType { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string X { get; set; }
        public string Y { get; set; }
        public bool IsLatitudeMissing { get; set; }
        public bool IsLongitudeMissing { get; set; }
    }
}