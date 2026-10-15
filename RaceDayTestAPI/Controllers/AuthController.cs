using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayTestAPI.Data;
using RaceDayTestAPI.DTOs;
using RaceDayTestAPI.Models;

namespace RaceDayTestAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly RaceDayTestDbContext _context;
        private readonly PasswordHasher<object> _passwordHasher;

        public AuthController(RaceDayTestDbContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<object>();
        }


        // POST: api/Auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.FirstName) ||
                string.IsNullOrWhiteSpace(request.LastName) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.Role))
            {
                return BadRequest("Please complete all required fields.");
            }

            string role = request.Role.Trim().ToLower();
            string email = request.Email.Trim().ToLower();

            if (role != "participant" && role != "organiser")
            {
                return BadRequest(
                    "Role must be Participant or Organiser.");
            }


            // Check if email already exists
            bool organiserExists = await _context.Organisers
                .AnyAsync(o => o.Email == email);

            bool participantExists = await _context.Participants
                .AnyAsync(p => p.Email == email);

            if (organiserExists || participantExists)
            {
                return Conflict("An account with this email already exists.");
            }


            // Register Participant
            if (role == "participant")
            {
                var participant = new Participant
                {
                    FirstName = request.FirstName.Trim(),
                    LastName = request.LastName.Trim(),
                    Email = email,
                    Phone = request.Phone.Trim(),
                    DateOfBirth = request.DateOfBirth,
                    CreatedAt = DateTime.Now
                };

                participant.PasswordHash =
                    _passwordHasher.HashPassword(
                        participant,
                        request.Password);

                _context.Participants.Add(participant);

                await _context.SaveChangesAsync();

                return StatusCode(201, new
                {
                    message = "Participant registered successfully.",
                    participantID = participant.ParticipantID,
                    role = "Participant"
                });
            }


            // Register Organiser
            var organiser = new Organiser
            {
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = email,
                Phone = request.Phone.Trim(),
                CreatedAt = DateTime.Now
            };

            organiser.PasswordHash =
                _passwordHasher.HashPassword(
                    organiser,
                    request.Password);

            _context.Organisers.Add(organiser);

            await _context.SaveChangesAsync();

            return StatusCode(201, new
            {
                message = "Organiser registered successfully.",
                organiserID = organiser.OrganiserID,
                role = "Organiser"
            });
        }


        // POST: api/Auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.Role))
            {
                return BadRequest("Email, password and role are required.");
            }

            string email = request.Email.Trim().ToLower();
            string role = request.Role.Trim().ToLower();


            // Participant login
            if (role == "participant")
            {
                var participant = await _context.Participants
                    .FirstOrDefaultAsync(p => p.Email == email);

                if (participant == null)
                {
                    return Unauthorized("Invalid email or password.");
                }

                var passwordResult =
                    _passwordHasher.VerifyHashedPassword(
                        participant,
                        participant.PasswordHash,
                        request.Password);

                if (passwordResult == PasswordVerificationResult.Failed)
                {
                    return Unauthorized("Invalid email or password.");
                }

                HttpContext.Session.SetInt32(
                    "ParticipantID",
                    participant.ParticipantID);

                HttpContext.Session.SetString(
                    "Role",
                    "Participant");

                return Ok(new
                {
                    message = "Participant login successful.",
                    participantID = participant.ParticipantID,
                    role = "Participant"
                });
            }


            // Organiser login
            if (role == "organiser")
            {
                var organiser = await _context.Organisers
                    .FirstOrDefaultAsync(o => o.Email == email);

                if (organiser == null)
                {
                    return Unauthorized("Invalid email or password.");
                }

                var passwordResult =
                    _passwordHasher.VerifyHashedPassword(
                        organiser,
                        organiser.PasswordHash,
                        request.Password);

                if (passwordResult == PasswordVerificationResult.Failed)
                {
                    return Unauthorized("Invalid email or password.");
                }

                HttpContext.Session.SetInt32(
                    "OrganiserID",
                    organiser.OrganiserID);

                HttpContext.Session.SetString(
                    "Role",
                    "Organiser");

                return Ok(new
                {
                    message = "Organiser login successful.",
                    organiserID = organiser.OrganiserID,
                    role = "Organiser"
                });
            }

            return BadRequest(
                "Role must be Participant or Organiser.");
        }


        // POST: api/Auth/logout
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            return NoContent();
        }
    }
}
