using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDayTestAPI.Models
{
    public class Enrolment
    {
        [Key]
        public int EnrolmentID { get; set; }

        [Required]
        public int ParticipantID { get; set; }

        [Required]
        public int EventID { get; set; }

        [Required]
        public int CategoryID { get; set; }

        public DateTime EnrolmentDate { get; set; } = DateTime.Now;

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Enrolled";

        [ForeignKey("ParticipantID")]
        public Participant Participant { get; set; } = null!;

        [ForeignKey("EventID")]
        public Event Event { get; set; } = null!;

        [ForeignKey("CategoryID")]
        public Category Category { get; set; } = null!;

        public Result? Result { get; set; }
    }
}
