using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayTestAPI.Controllers;
using RaceDayTestAPI.Data;
using RaceDayTestAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading; 
using System.Threading.Tasks;
using RaceDayTestAPI.DTOs;


namespace RaceDayTestAPI.Test
{
    public class AuthControllerTests
    {
        private readonly RaceDayTestDbContext _context;
        private readonly AuthController _controller;
        private readonly Dictionary<string, byte[]> _values = new();
        public AuthControllerTests()
        {
            var options = new DbContextOptionsBuilder<RaceDayTestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new RaceDayTestDbContext(options);

            var httpContext = new DefaultHttpContext();
            httpContext.Session = new TestSession();

            _controller = new AuthController(_context)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = httpContext
                }
            };
        }

        private RegisterRequest CreateRegisterRequest()
        {
            return new RegisterRequest
            {
                FirstName = "John",
                LastName = "Smith",
                Email = "john@example.com",
                Password = "TestPassword123!",
                Phone = "0712345678",
                Role = "participant"
            };
        }

        private async Task AddParticipant()
        {
            var participant = new Participant
            {
                FirstName = "John",
                LastName = "Smith",
                Email = "john@example.com",
                Phone = "0712345678",
                PasswordHash = new PasswordHasher<object>()
                    .HashPassword(new object(), "TestPassword123!")
            };

            _context.Participants.Add(participant);
            await _context.SaveChangesAsync();
        }

        [Fact]
        public async Task Register_ValidParticipant_Returns201()
        {
            var request = CreateRegisterRequest();

            var result = await _controller.Register(request);

            var response = Assert.IsType<ObjectResult>(result);
            Assert.Equal(201, response.StatusCode);
            Assert.Single(_context.Participants);
        }

        [Fact]
        public async Task Register_MissingRequiredFields_Returns400()
        {
            var request = CreateRegisterRequest();
            request.FirstName = "";

            var result = await _controller.Register(request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Register_InvalidRole_Returns400()
        {
            var request = CreateRegisterRequest();
            request.Role = "visitor";

            var result = await _controller.Register(request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Register_ExistingEmail_Returns409()
        {
            await AddParticipant();

            var request = CreateRegisterRequest();

            var result = await _controller.Register(request);

            Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task Login_ValidParticipant_Returns200()
        {
            await AddParticipant();

            var request = new LoginRequest
            {
                Email = "john@example.com",
                Password = "TestPassword123!",
                Role = "participant"
            };

            var result = await _controller.Login(request);

            Assert.IsType<OkObjectResult>(result);

            Assert.Equal(
                1,
                _controller.HttpContext.Session.GetInt32("ParticipantID"));

            Assert.Equal(
                "Participant",
                _controller.HttpContext.Session.GetString("Role"));
        }

        [Fact]
        public async Task Login_WrongPassword_Returns401()
        {
            await AddParticipant();

            var request = new LoginRequest
            {
                Email = "john@example.com",
                Password = "WrongPassword!",
                Role = "participant"
            };

            var result = await _controller.Login(request);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task Login_AccountDoesNotExist_Returns401()
        {
            var request = new LoginRequest
            {
                Email = "unknown@example.com",
                Password = "TestPassword123!",
                Role = "participant"
            };

            var result = await _controller.Login(request);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public void Logout_ClearsSession_Returns204()
        {
            _controller.HttpContext.Session.SetInt32("ParticipantID", 1);
            _controller.HttpContext.Session.SetString("Role", "Participant");

            var result = _controller.Logout();

            Assert.IsType<NoContentResult>(result);

            Assert.Null(
                _controller.HttpContext.Session.GetInt32("ParticipantID"));

            Assert.Null(
                _controller.HttpContext.Session.GetString("Role"));
        }
    }

    // Simple session implementation for unit tests
    public class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _values = new();

        public string Id { get; } = Guid.NewGuid().ToString();

        public bool IsAvailable => true;

        public IEnumerable<string> Keys => _values.Keys;

        public void Clear()
        {
            _values.Clear();
        }

        public Task CommitAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task LoadAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public void Remove(string key)
        {
            _values.Remove(key);
        }

        public void Set(string key, byte[] value)
        {
            _values[key] = value;
        }

        public bool TryGetValue(string key, out byte[] value)
        {
            return _values.TryGetValue(key, out value!);
        }

    }
}
 
