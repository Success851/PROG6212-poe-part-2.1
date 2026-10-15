using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayTestAPI.Data;
using RaceDayTestAPI.DTOs;
using RaceDayTestAPI.Models;

namespace RaceDayTestAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly RaceDayTestDbContext _context;

        public EventsController(RaceDayTestDbContext context)
        {
            _context = context;
        }


        // GET: api/Events
        // View all events
        [HttpGet]
        public async Task<IActionResult> GetEvents()
        {
            var events = await _context.Events
                .Include(e => e.Organiser)
                .Select(e => new
                {
                    e.EventID,
                    e.EventName,
                    e.Description,
                    e.EventDate,
                    e.Location,
                    e.DistanceKm,
                    e.EventType,
                    e.CreatedAt,

                    Organiser = new
                    {
                        e.Organiser.OrganiserID,
                        e.Organiser.FirstName,
                        e.Organiser.LastName
                    }
                })
                .ToListAsync();

            return Ok(events);
        }


        // GET: api/Events/5
        // View one event
        [HttpGet("{id}")]
        public async Task<IActionResult> GetEvent(int id)
        {
            var eventItem = await _context.Events
                .Include(e => e.Organiser)
                .Include(e => e.Categories)
                .FirstOrDefaultAsync(e => e.EventID == id);

            if (eventItem == null)
            {
                return NotFound("Event not found.");
            }

            return Ok(new
            {
                eventItem.EventID,
                eventItem.EventName,
                eventItem.Description,
                eventItem.EventDate,
                eventItem.Location,
                eventItem.DistanceKm,
                eventItem.EventType,
                eventItem.CreatedAt,

                Organiser = new
                {
                    eventItem.Organiser.OrganiserID,
                    eventItem.Organiser.FirstName,
                    eventItem.Organiser.LastName
                },

                Categories = eventItem.Categories.Select(c => new
                {
                    c.CategoryID,
                    c.CategoryName,
                    c.CategoryType,
                    c.MinAge,
                    c.MaxAge,
                    c.DistanceKm,
                    c.Description
                })
            });
        }


        // POST: api/Events
        // Organiser creates an event
        [HttpPost]
        public async Task<IActionResult> CreateEvent(
            CreateEventRequest request)
        {
            var organiserID =
                HttpContext.Session.GetInt32("OrganiserID");

            var role =
                HttpContext.Session.GetString("Role");

            if (organiserID == null || role != "Organiser")
            {
                return Unauthorized(
                    "Please log in as an organiser.");
            }

            if (string.IsNullOrWhiteSpace(request.EventName) ||
                string.IsNullOrWhiteSpace(request.Location) ||
                string.IsNullOrWhiteSpace(request.EventType))
            {
                return BadRequest(
                    "Event name, location and event type are required.");
            }

            if (request.DistanceKm <= 0)
            {
                return BadRequest(
                    "Distance must be greater than zero.");
            }

            if (request.EventDate <= DateTime.Now)
            {
                return BadRequest(
                    "Event date must be in the future.");
            }

            string eventType = request.EventType.Trim();

            if (eventType != "Run" &&
                eventType != "Walk" &&
                eventType != "Cycle")
            {
                return BadRequest(
                    "Event type must be Run, Walk or Cycle.");
            }

            var newEvent = new Event
            {
                OrganiserID = organiserID.Value,
                EventName = request.EventName.Trim(),
                Description = request.Description.Trim(),
                EventDate = request.EventDate,
                Location = request.Location.Trim(),
                DistanceKm = request.DistanceKm,
                EventType = eventType,
                CreatedAt = DateTime.Now
            };

            _context.Events.Add(newEvent);

            await _context.SaveChangesAsync();

            return StatusCode(201, new
            {
                message = "Event created successfully.",
                eventID = newEvent.EventID
            });
        }


        // PUT: api/Events/5
        // Organiser updates their own event
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEvent(
            int id,
            UpdateEventRequest request)
        {
            var organiserID =
                HttpContext.Session.GetInt32("OrganiserID");

            var role =
                HttpContext.Session.GetString("Role");

            if (organiserID == null || role != "Organiser")
            {
                return Unauthorized(
                    "Please log in as an organiser.");
            }

            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == id);

            if (eventItem == null)
            {
                return NotFound("Event not found.");
            }

            if (eventItem.OrganiserID != organiserID.Value)
            {
                return Forbid(
                    "You can only update your own events.");
            }

            if (string.IsNullOrWhiteSpace(request.EventName) ||
                string.IsNullOrWhiteSpace(request.Location) ||
                string.IsNullOrWhiteSpace(request.EventType))
            {
                return BadRequest(
                    "Event name, location and event type are required.");
            }

            if (request.DistanceKm <= 0)
            {
                return BadRequest(
                    "Distance must be greater than zero.");
            }

            string eventType = request.EventType.Trim();

            if (eventType != "Run" &&
                eventType != "Walk" &&
                eventType != "Cycle")
            {
                return BadRequest(
                    "Event type must be Run, Walk or Cycle.");
            }

            eventItem.EventName = request.EventName.Trim();
            eventItem.Description = request.Description.Trim();
            eventItem.EventDate = request.EventDate;
            eventItem.Location = request.Location.Trim();
            eventItem.DistanceKm = request.DistanceKm;
            eventItem.EventType = eventType;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Event updated successfully."
            });
        }


        // DELETE: api/Events/5
        // Organiser deletes their own event
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var organiserID =
                HttpContext.Session.GetInt32("OrganiserID");

            var role =
                HttpContext.Session.GetString("Role");

            if (organiserID == null || role != "Organiser")
            {
                return Unauthorized(
                    "Please log in as an organiser.");
            }

            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == id);

            if (eventItem == null)
            {
                return NotFound("Event not found.");
            }

            if (eventItem.OrganiserID != organiserID.Value)
            {
                return Forbid(
                    "You can only delete your own events.");
            }

            _context.Events.Remove(eventItem);

            await _context.SaveChangesAsync();

            return NoContent();
        }


        // GET: api/Events/5/enrolments
        // Organiser views participants enrolled in their event
        [HttpGet("{eventId}/enrolments")]
        public async Task<IActionResult> GetEventEnrolments(
            int eventId)
        {
            var organiserID =
                HttpContext.Session.GetInt32("OrganiserID");

            var role =
                HttpContext.Session.GetString("Role");

            if (organiserID == null || role != "Organiser")
            {
                return Unauthorized(
                    "Please log in as an organiser.");
            }

            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e =>
                    e.EventID == eventId);

            if (eventItem == null)
            {
                return NotFound("Event not found.");
            }

            if (eventItem.OrganiserID != organiserID.Value)
            {
                return Forbid(
                    "You can only view enrolments for your own events.");
            }

            var enrolments = await _context.Enrolments
                .Where(e => e.EventID == eventId)
                .Include(e => e.Participant)
                .Include(e => e.Category)
                .Select(e => new
                {
                    e.EnrolmentID,
                    e.EnrolmentDate,
                    e.Status,

                    Participant = new
                    {
                        e.Participant.ParticipantID,
                        e.Participant.FirstName,
                        e.Participant.LastName,
                        e.Participant.Email,
                        e.Participant.Phone
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
    }
}
