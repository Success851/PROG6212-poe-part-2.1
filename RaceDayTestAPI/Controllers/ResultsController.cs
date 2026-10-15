using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayTestAPI.Data;
using RaceDayTestAPI.DTOs;

namespace RaceDayTestAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ResultsController : ControllerBase
    {
        private readonly RaceDayTestDbContext _context;

        public ResultsController(RaceDayTestDbContext context)
        {
            _context = context;
        }

        // POST: api/Enrolments/5/result
        [HttpPost("/api/enrolments/{enrolmentId}/result")]
        public async Task<IActionResult> CreateResult(
            int enrolmentId,
            CreateResultRequest request)
        {
            var organiserID =
                HttpContext.Session.GetInt32("OrganiserID");

            var role =
                HttpContext.Session.GetString("Role");

            if (organiserID == null || role != "Organiser")
                return Unauthorized(
                    "Please log in as an organiser.");

            // Find the enrolment together with its event
            var enrolment = await _context.Enrolments
                .Include(e => e.Event)
                .Include(e => e.Participant)
                .FirstOrDefaultAsync(e =>
                    e.EnrolmentID == enrolmentId);

            if (enrolment == null)
                return NotFound("Enrolment not found.");

            // Check that the organiser owns the event
            if (enrolment.Event.OrganiserID != organiserID.Value)
                return Forbid(
                    "You can only record results for your own events.");

            // Check enrolment status
            if (enrolment.Status != "Enrolled")
                return BadRequest(
                    "A result can only be recorded for an active enrolment.");

            // Check if a result already exists
            var existingResult = await _context.Results
                .FirstOrDefaultAsync(r =>
                    r.EnrolmentID == enrolmentId);

            if (existingResult != null)
                return Conflict(
                    "A result already exists for this enrolment.");

            // Validate result status
            string resultStatus = request.ResultStatus.Trim();

            if (resultStatus != "Completed" &&
                resultStatus != "Did Not Finish" &&
                resultStatus != "Disqualified")
            {
                return BadRequest(
                    "Result status must be Completed, Did Not Finish or Disqualified.");
            }

            // Validate position
            if (request.Position.HasValue &&
                request.Position.Value <= 0)
            {
                return BadRequest(
                    "Position must be greater than zero.");
            }

            var result = new Models.Result
            {
                EnrolmentID = enrolmentId,
                FinishTime = request.FinishTime,
                Position = request.Position,
                ResultStatus = resultStatus,
                RecordedAt = DateTime.Now
            };

            _context.Results.Add(result);

            await _context.SaveChangesAsync();

            return StatusCode(201, new
            {
                message = "Result recorded successfully.",
                resultID = result.ResultID,
                enrolmentID = result.EnrolmentID,
                finishTime = result.FinishTime,
                position = result.Position,
                resultStatus = result.ResultStatus
            });
        }

        // PUT: api/Enrolments/5/result
        [HttpPut("/api/enrolments/{enrolmentId}/result")]
        public async Task<IActionResult> UpdateResult(
            int enrolmentId,
            UpdateResultRequest request)
        {
            var organiserID =
                HttpContext.Session.GetInt32("OrganiserID");

            var role =
                HttpContext.Session.GetString("Role");

            if (organiserID == null || role != "Organiser")
                return Unauthorized(
                    "Please log in as an organiser.");

            var result = await _context.Results
                .Include(r => r.Enrolment)
                .ThenInclude(e => e.Event)
                .FirstOrDefaultAsync(r =>
                    r.EnrolmentID == enrolmentId);

            if (result == null)
                return NotFound(
                    "Result not found.");

            // Check event ownership
            if (result.Enrolment.Event.OrganiserID != organiserID.Value)
                return Forbid(
                    "You can only update results for your own events.");

            string resultStatus = request.ResultStatus.Trim();

            if (resultStatus != "Completed" &&
                resultStatus != "Did Not Finish" &&
                resultStatus != "Disqualified")
            {
                return BadRequest(
                    "Result status must be Completed, Did Not Finish or Disqualified.");
            }

            if (request.Position.HasValue &&
                request.Position.Value <= 0)
            {
                return BadRequest(
                    "Position must be greater than zero.");
            }

            result.FinishTime = request.FinishTime;
            result.Position = request.Position;
            result.ResultStatus = resultStatus;
            result.RecordedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Result updated successfully."
            });
        }

        // GET: api/Results/my-results
        [HttpGet("my-results")]
        public async Task<IActionResult> GetMyResults()
        {
            var participantID =
                HttpContext.Session.GetInt32("ParticipantID");

            var role =
                HttpContext.Session.GetString("Role");

            if (participantID == null || role != "Participant")
                return Unauthorized(
                    "Please log in as a participant.");

            var results = await _context.Results
                .Include(r => r.Enrolment)
                .ThenInclude(e => e.Event)
                .Include(r => r.Enrolment)
                .ThenInclude(e => e.Category)
                .Where(r =>
                    r.Enrolment.ParticipantID == participantID.Value)
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
                        r.Enrolment.Event.EventDate,
                        r.Enrolment.Event.Location,
                        r.Enrolment.Event.EventType
                    },

                    Category = new
                    {
                        r.Enrolment.Category.CategoryID,
                        r.Enrolment.Category.CategoryName,
                        r.Enrolment.Category.CategoryType
                    }
                })
                .ToListAsync();

            return Ok(results);
        }

        // GET: api/Events/5/results
        [HttpGet("/api/events/{eventId}/results")]
        public async Task<IActionResult> GetEventResults(int eventId)
        {
            var organiserID =
                HttpContext.Session.GetInt32("OrganiserID");

            var role =
                HttpContext.Session.GetString("Role");

            if (organiserID == null || role != "Organiser")
                return Unauthorized(
                    "Please log in as an organiser.");

            // Check event
            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e =>
                    e.EventID == eventId);

            if (eventItem == null)
                return NotFound("Event not found.");

            // Check event ownership
            if (eventItem.OrganiserID != organiserID.Value)
                return Forbid(
                    "You can only view results for your own events.");

            var results = await _context.Results
                .Include(r => r.Enrolment)
                .ThenInclude(e => e.Participant)
                .Include(r => r.Enrolment)
                .ThenInclude(e => e.Category)
                .Where(r =>
                    r.Enrolment.EventID == eventId)
                .Select(r => new
                {
                    r.ResultID,
                    r.FinishTime,
                    r.Position,
                    r.ResultStatus,
                    r.RecordedAt,

                    Participant = new
                    {
                        r.Enrolment.Participant.ParticipantID,
                        r.Enrolment.Participant.FirstName,
                        r.Enrolment.Participant.LastName,
                        r.Enrolment.Participant.Email
                    },

                    Category = new
                    {
                        r.Enrolment.Category.CategoryID,
                        r.Enrolment.Category.CategoryName,
                        r.Enrolment.Category.CategoryType
                    }
                })
                .OrderBy(r => r.Position)
                .ToListAsync();

            return Ok(results);
        }
    }
}
