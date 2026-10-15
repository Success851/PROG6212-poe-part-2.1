
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayTestAPI.Controllers;
using RaceDayTestAPI.Data;
using RaceDayTestAPI.DTOs;
using RaceDayTestAPI.Models;
using Xunit;

namespace RaceDayTestAPI.Test
{
    public class ParticipantsControllerTests
    {
        private readonly RaceDayTestDbContext _context;
        private readonly ParticipantsController _controller;
        private readonly TestSession _session;

        public ParticipantsControllerTests()
        {
            var options = new DbContextOptionsBuilder<RaceDayTestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new RaceDayTestDbContext(options);
            _session = new TestSession();

            var httpContext = new DefaultHttpContext();
            httpContext.Session = _session;

            _controller = new ParticipantsController(_context)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = httpContext
                }
            };
        }

        private async Task AddParticipant()
        {
            _context.Participants.Add(new Participant
            {
                ParticipantID = 1,
                FirstName = "John",
                LastName = "Smith",
                Email = "john@example.com",
                PasswordHash = "test-hash",
                Phone = "0712345678",
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
        }

        private void LoginAsParticipant()
        {
            _session.SetInt32("ParticipantID", 1);
            _session.SetString("Role", "Participant");
        }

        [Fact]
        public async Task GetProfile_WhenNotLoggedIn_ReturnsUnauthorized()
        {
            var result = await _controller.GetProfile();

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task GetProfile_WhenLoggedIn_ReturnsProfile()
        {
            await AddParticipant();
            LoginAsParticipant();

            var result = await _controller.GetProfile();

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetProfile_WhenParticipantDoesNotExist_ReturnsNotFound()
        {
            LoginAsParticipant();

            var result = await _controller.GetProfile();

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task UpdateProfile_WhenNotLoggedIn_ReturnsUnauthorized()
        {
            var request = new UpdateProfileRequest
            {
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane@example.com",
                Phone = "0722222222"
            };

            var result = await _controller.UpdateProfile(request);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task UpdateProfile_WithValidDetails_ReturnsOk()
        {
            await AddParticipant();
            LoginAsParticipant();

            var request = new UpdateProfileRequest
            {
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane@example.com",
                Phone = "0722222222"
            };

            var result = await _controller.UpdateProfile(request);

            Assert.IsType<OkObjectResult>(result);

            var participant = await _context.Participants
                .FirstAsync(p => p.ParticipantID == 1);

            Assert.Equal("Jane", participant.FirstName);
            Assert.Equal("jane@example.com", participant.Email);
        }

        [Fact]
        public async Task UpdateProfile_WithMissingRequiredDetails_ReturnsBadRequest()
        {
            await AddParticipant();
            LoginAsParticipant();

            var request = new UpdateProfileRequest
            {
                FirstName = "",
                LastName = "Smith",
                Email = "john@example.com",
                Phone = "0712345678"
            };

            var result = await _controller.UpdateProfile(request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetEnrolments_WhenNotLoggedIn_ReturnsUnauthorized()
        {
            var result = await _controller.GetEnrolments();

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task GetEnrolments_WhenLoggedIn_ReturnsOk()
        {
            await AddParticipant();
            LoginAsParticipant();

            var result = await _controller.GetEnrolments();

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetResults_WhenNotLoggedIn_ReturnsUnauthorized()
        {
            var result = await _controller.GetResults();

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task GetResults_WhenLoggedIn_ReturnsOk()
        {
            await AddParticipant();
            LoginAsParticipant();

            var result = await _controller.GetResults();

            Assert.IsType<OkObjectResult>(result);
        }
    }
}
