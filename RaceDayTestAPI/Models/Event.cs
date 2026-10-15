using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDayTestAPI.Models
{
    public class Event
    {
        [Key]
        public int EventID { get; set; }

        [Required]
        public int OrganiserID { get; set; }

        [Required]
        [MaxLength(100)]
        public string EventName { get; set; } = "";

        [MaxLength(255)]
        public string Description { get; set; } = "";

        [Required]
        public DateTime EventDate { get; set; }

        [Required]
        [MaxLength(100)]
        public string Location { get; set; } = "";

        [Column(TypeName = "decimal(5,2)")]
        public decimal DistanceKm { get; set; }

        [Required]
        [MaxLength(20)]
        public string EventType { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("OrganiserID")]
        public Organiser Organiser { get; set; } = null!;

        public ICollection<Category> Categories { get; set; } =
            new List<Category>();

        public ICollection<Enrolment> Enrolments { get; set; } =
            new List<Enrolment>();
    }
}
