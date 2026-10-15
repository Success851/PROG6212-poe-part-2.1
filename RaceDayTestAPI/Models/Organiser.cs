using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace RaceDayTestAPI.Models
{
    public class Organiser
    {
        [Key]
        public int OrganiserID { get; set; }

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

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<Event> Events { get; set; } = new List<Event>();
    }
}
