
namespace SmartX.Client.Models
{
    public class DeviceRecommendationDto
    {
        public int Id { get; set; }

        public string MacAddress { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string RecommendedAction { get; set; } = string.Empty;

        public string Severity { get; set; } = "Info";

        public string Reason { get; set; } = string.Empty;

        public DateTime GeneratedAt { get; set; }
    }

    public class RecommendationsResponseDto
    {
        public int Count { get; set; }

        public DateTime GeneratedAt { get; set; }

        public List<DeviceRecommendationDto> Recommendations { get; set; } = new();
    }
}
