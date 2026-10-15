using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayTestAPI.Data;
using RaceDayTestAPI.DTOs;

namespace RaceDayTestAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EnrolmentsController : ControllerBase
    {
        private readonly RaceDayTestDbContext _context;

        public EnrolmentsController(RaceDayTestDbContext context)
        {
            _context = context;
        }

        // POST: api/Enrolments/events/5/enrolments
        [HttpPost("/api/events/{eventId}/enrolments")]
        public async Task<IActionResult> CreateEnrolment(
            int eventId,
            EnrolmentRequest request)
        {
            var participantID =
                HttpContext.Session.GetInt32("ParticipantID");

            var role =
                HttpContext.Session.GetString("Role");

            if (participantID == null || role != "Participant")
                return Unauthorized(
                    "Please log in as a participant.");

            // Check that the event exists
            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == eventId);

            if (eventItem == null)
                return NotFound("Event not found.");

            // Check that the event has not already taken place
            if (eventItem.EventDate <= DateTime.Now)
                return BadRequest(
                    "You cannot enrol in an event that has already taken place.");

            // Check that the category exists
            var category = await _context.Categories
                .FirstOrDefaultAsync(c =>
                    c.CategoryID == request.CategoryID &&
                    c.EventID == eventId);

            if (category == null)
                return BadRequest(
                    "The selected category does not belong to this event.");

            // Check if participant is already enrolled
            bool alreadyEnrolled = await _context.Enrolments
                .AnyAsync(e =>
                    e.ParticipantID == participantID.Value &&
                    e.EventID == eventId);

            if (alreadyEnrolled)
                return Conflict(
                    "You are already enrolled in this event.");

            var enrolment = new Models.Enrolment
            {
                ParticipantID = participantID.Value,
                EventID = eventId,
                CategoryID = request.CategoryID,
                EnrolmentDate = DateTime.Now,
                Status = "Enrolled"
            };

            _context.Enrolments.Add(enrolment);

            await _context.SaveChangesAsync();

            return StatusCode(201, new
            {
                message = "Enrolment created successfully.",
                enrolmentID = enrolment.EnrolmentID,
                eventID = enrolment.EventID,
                categoryID = enrolment.CategoryID,
                status = enrolment.Status
            });
        }

   

        // DELETE: api/Enrolments/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> CancelEnrolment(int id)
        {
            var participantID =
                HttpContext.Session.GetInt32("ParticipantID");

            var role =
                HttpContext.Session.GetString("Role");

            if (participantID == null || role != "Participant")
                return Unauthorized(
                    "Please log in as a participant.");

            var enrolment = await _context.Enrolments
                .FirstOrDefaultAsync(e =>
                    e.EnrolmentID == id &&
                    e.ParticipantID == participantID.Value);

            if (enrolment == null)
                return NotFound(
                    "Enrolment not found.");

            if (enrolment.Status == "Cancelled")
                return BadRequest(
                    "This enrolment has already been cancelled.");

            enrolment.Status = "Cancelled";

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
