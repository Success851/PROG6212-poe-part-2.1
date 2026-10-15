namespace RaceDayTestAPI.DTOs
{
    public class CreateResultRequest
    {
        public TimeSpan? FinishTime { get; set; }

        public int? Position { get; set; }

        public string ResultStatus { get; set; } = "";
    }
}
