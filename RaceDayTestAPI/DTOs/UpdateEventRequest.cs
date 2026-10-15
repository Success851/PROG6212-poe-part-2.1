namespace RaceDayTestAPI.DTOs
{
    public class UpdateEventRequest
    {
        public string EventName { get; set; } = "";

        public string Description { get; set; } = "";

        public DateTime EventDate { get; set; }

        public string Location { get; set; } = "";

        public decimal DistanceKm { get; set; }

        public string EventType { get; set; } = "";
    }
}
