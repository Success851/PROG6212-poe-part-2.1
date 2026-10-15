namespace RaceDayTestAPI.DTOs
{
    public class UpdateResultRequest
    {
        public TimeSpan? FinishTime { get; set; }

        public int? Position { get; set; }

        public string ResultStatus { get; set; } = "";
    }
}
