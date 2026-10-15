using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayTestAPI.Data;
using RaceDayTestAPI.DTOs;

namespace RaceDayTestAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrganisersController : ControllerBase
    {
        private readonly RaceDayTestDbContext _context;

        public OrganisersController(RaceDayTestDbContext context)
        {
            _context = context;
        }


        // GET: api/Organisers/profile
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var organiserID = HttpContext.Session.GetInt32("OrganiserID");

            var role = HttpContext.Session.GetString("Role");

            if (organiserID == null || role != "Organiser")
            {
                return Unauthorized("Please log in as an organiser.");
            }

            var organiser = await _context.Organisers
                .FirstOrDefaultAsync(o => o.OrganiserID == organiserID);

            if (organiser == null)
            {
                return NotFound("Organiser not found.");
            }

            return Ok(new
            {
                organiser.OrganiserID,
                organiser.FirstName,
                organiser.LastName,
                organiser.Email,
                organiser.Phone,
                organiser.CreatedAt
            });
        }


        // PUT: api/Organisers/profile
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile(
            UpdateProfileRequest request)
        {
            var organiserID = HttpContext.Session.GetInt32("OrganiserID");

            var role = HttpContext.Session.GetString("Role");

            if (organiserID == null || role != "Organiser")
            {
                return Unauthorized("Please log in as an organiser.");
            }

            var organiser = await _context.Organisers
                .FirstOrDefaultAsync(o => o.OrganiserID == organiserID);

            if (organiser == null)
            {
                return NotFound("Organiser not found.");
            }

            if (string.IsNullOrWhiteSpace(request.FirstName) ||
                string.IsNullOrWhiteSpace(request.LastName) ||
                string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(
                    "First name, last name and email are required.");
            }

            string email = request.Email.Trim().ToLower();

            bool emailExists = await _context.Organisers
                .AnyAsync(o =>
                    o.Email == email &&
                    o.OrganiserID != organiserID);

            if (emailExists)
            {
                return Conflict("That email is already being used.");
            }

            organiser.FirstName = request.FirstName.Trim();
            organiser.LastName = request.LastName.Trim();
            organiser.Email = email;
            organiser.Phone = request.Phone.Trim();

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Organiser profile updated successfully."
            });
        }
    }
}
