using System.ComponentModel.DataAnnotations;

namespace RaceDayTestAPI.Models
{
    public class Participant
    {
        [Key]
        public int ParticipantID { get; set; }

        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; } = "";

        [Required]
        [MaxLength(50)]
        public string LastName { get; set; } = "";

        [Required]
        [MaxLength(100)]
        public string Email { get; set; } = "";

        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; } = "";

        [MaxLength(20)]
        public string Phone { get; set; } = "";

        public DateTime? DateOfBirth { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<Enrolment> Enrolments { get; set; } =
            new List<Enrolment>();
    }
}
