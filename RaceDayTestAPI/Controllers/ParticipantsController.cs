using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayTestAPI.Data;
using RaceDayTestAPI.DTOs;

namespace RaceDayTestAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ParticipantsController : ControllerBase
    {
        private readonly RaceDayTestDbContext _context;

        public ParticipantsController(RaceDayTestDbContext context)
        {
            _context = context;
        }


        // GET: api/Participants/profile
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var participantID = HttpContext.Session.GetInt32("ParticipantID");

            var role = HttpContext.Session.GetString("Role");

            if (participantID == null || role != "Participant")
            {
                return Unauthorized("Please log in as a participant.");
            }

            var participant = await _context.Participants
                .FirstOrDefaultAsync(p => p.ParticipantID == participantID);

            if (participant == null)
            {
                return NotFound("Participant not found.");
            }

            return Ok(new
            {
                participant.ParticipantID,
                participant.FirstName,
                participant.LastName,
                participant.Email,
                participant.Phone,
                participant.DateOfBirth,
                participant.CreatedAt
            });
        }


        // PUT: api/Participants/profile
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile(
            UpdateProfileRequest request)
        {
            var participantID = HttpContext.Session.GetInt32("ParticipantID");

            var role = HttpContext.Session.GetString("Role");

            if (participantID == null || role != "Participant")
            {
                return Unauthorized("Please log in as a participant.");
            }

            var participant = await _context.Participants
                .FirstOrDefaultAsync(p => p.ParticipantID == participantID);

            if (participant == null)
            {
                return NotFound("Participant not found.");
            }

            if (string.IsNullOrWhiteSpace(request.FirstName) ||
                string.IsNullOrWhiteSpace(request.LastName) ||
                string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(
                    "First name, last name and email are required.");
            }

            string email = request.Email.Trim().ToLower();

            bool emailExists = await _context.Participants
                .AnyAsync(p =>
                    p.Email == email &&
                    p.ParticipantID != participantID);

            if (emailExists)
            {
                return Conflict("That email is already being used.");
            }

            participant.FirstName = request.FirstName.Trim();
            participant.LastName = request.LastName.Trim();
            participant.Email = email;
            participant.Phone = request.Phone.Trim();

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Participant profile updated successfully."
            });
        }


        // GET: api/Participants/enrolments
        [HttpGet("enrolments")]
        public async Task<IActionResult> GetEnrolments()
        {
            var participantID = HttpContext.Session.GetInt32("ParticipantID");

            var role = HttpContext.Session.GetString("Role");

            if (participantID == null || role != "Participant")
            {
                return Unauthorized("Please log in as a participant.");
            }

            var enrolments = await _context.Enrolments
                .Where(e => e.ParticipantID == participantID)
                .Include(e => e.Event)
                .Include(e => e.Category)
                .Select(e => new
                {
                    e.EnrolmentID,
                    e.EnrolmentDate,
                    e.Status,

                    Event = new
                    {
                        e.Event.EventID,
                        e.Event.EventName,
                        e.Event.EventDate,
                        e.Event.Location
                    },

                    Category = new
                    {
                        e.Category.CategoryID,
                        e.Category.CategoryName,
                        e.Category.CategoryType
                    }
                })
                .ToListAsync();

            return Ok(enrolments);
        }


        // GET: api/Participants/results
        [HttpGet("results")]
        public async Task<IActionResult> GetResults()
        {
            var participantID = HttpContext.Session.GetInt32("ParticipantID");

            var role = HttpContext.Session.GetString("Role");

            if (participantID == null || role != "Participant")
            {
                return Unauthorized("Please log in as a participant.");
            }

            var results = await _context.Results
                .Include(r => r.Enrolment)
                .ThenInclude(e => e.Event)
                .Include(r => r.Enrolment)
                .ThenInclude(e => e.Category)
                .Where(r => r.Enrolment.ParticipantID == participantID)
                .Select(r => new
                {
                    r.ResultID,
                    r.FinishTime,
                    r.Position,
                    r.ResultStatus,
                    r.RecordedAt,

                    Event = new
                    {
                        r.Enrolment.Event.EventID,
                        r.Enrolment.Event.EventName,
                        r.Enrolment.Event.EventDate
                    },

                    Category = new
                    {
                        r.Enrolment.Category.CategoryID,
                        r.Enrolment.Category.CategoryName
                    }
                })
                .ToListAsync();

            return Ok(results);
        }
    }
}
