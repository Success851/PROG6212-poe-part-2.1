using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDayTestAPI.Models
{
    public class Category
    {
        [Key]
        public int CategoryID { get; set; }

        [Required]
        public int EventID { get; set; }

        [Required]
        [MaxLength(100)]
        public string CategoryName { get; set; } = "";

        [Required]
        [MaxLength(20)]
        public string CategoryType { get; set; } = "";

        public int? MinAge { get; set; }

        public int? MaxAge { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? DistanceKm { get; set; }

        [MaxLength(255)]
        public string Description { get; set; } = "";

        [ForeignKey("EventID")]
        public Event Event { get; set; } = null!;

        public ICollection<Enrolment> Enrolments { get; set; } =
            new List<Enrolment>();
    }
}
