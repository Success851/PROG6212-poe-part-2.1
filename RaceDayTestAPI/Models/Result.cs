using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDayTestAPI.Models
{
    public class Result
    {
        [Key]
        public int ResultID { get; set; }

        [Required]
        public int EnrolmentID { get; set; }

        public TimeSpan? FinishTime { get; set; }

        public int? Position { get; set; }

        [Required]
        [MaxLength(20)]
        public string ResultStatus { get; set; } = "";

        public DateTime RecordedAt { get; set; } = DateTime.Now;

        [ForeignKey("EnrolmentID")]
        public Enrolment Enrolment { get; set; } = null!;
    }
}
