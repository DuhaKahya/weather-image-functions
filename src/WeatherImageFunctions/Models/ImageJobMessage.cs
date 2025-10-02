namespace WeatherImageFunctions.Models
{

    public class ImageJobMessage
    {
        public string JobId { get; set; } = string.Empty;
        public string Query { get; set; } = string.Empty;
        public int StationId { get; set; }
        public string StationName { get; set; } = string.Empty;
        public double Temperature { get; set; }
        public int Humidity { get; set; }
    }
}
