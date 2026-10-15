namespace RaceDayTestAPI.DTOs
{
    public class UpdateCategoryRequest
    {
        public string CategoryName { get; set; } = "";

        public string CategoryType { get; set; } = "";

        public int? MinAge { get; set; }

        public int? MaxAge { get; set; }

        public decimal? DistanceKm { get; set; }

        public string Description { get; set; } = "";
    }
}
