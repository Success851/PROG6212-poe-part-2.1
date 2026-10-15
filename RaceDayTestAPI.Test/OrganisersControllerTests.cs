
using System;
using System.Collections.Generic;
using System.Threading;
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
    public class OrganisersControllerTests
    {
        private readonly RaceDayTestDbContext _context;
        private readonly OrganisersController _controller;
        private readonly TestSession _session;

        public OrganisersControllerTests()
        {
            var options = new DbContextOptionsBuilder<RaceDayTestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new RaceDayTestDbContext(options);
            _session = new TestSession();

            var httpContext = new DefaultHttpContext();
            httpContext.Session = _session;

            _controller = new OrganisersController(_context)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = httpContext
                }
            };
        }

        private async Task AddOrganiser()
        {
            _context.Organisers.Add(new Organiser
            {
                OrganiserID = 1,
                FirstName = "Mary",
                LastName = "Jones",
                Email = "mary@example.com",
                PasswordHash = "test-hash",
                Phone = "0712345678",
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
        }

        private void LoginAsOrganiser()
        {
            _session.SetInt32("OrganiserID", 1);
            _session.SetString("Role", "Organiser");
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
            await AddOrganiser();
            LoginAsOrganiser();

            var result = await _controller.GetProfile();

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task GetProfile_WhenOrganiserDoesNotExist_ReturnsNotFound()
        {
            LoginAsOrganiser();

            var result = await _controller.GetProfile();

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task UpdateProfile_WhenNotLoggedIn_ReturnsUnauthorized()
        {
            var request = new UpdateProfileRequest
            {
                FirstName = "Jane",
                LastName = "Jones",
                Email = "jane@example.com",
                Phone = "0722222222"
            };

            var result = await _controller.UpdateProfile(request);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task UpdateProfile_WithValidDetails_ReturnsOk()
        {
            await AddOrganiser();
            LoginAsOrganiser();

            var request = new UpdateProfileRequest
            {
                FirstName = "Jane",
                LastName = "Jones",
                Email = "jane@example.com",
                Phone = "0722222222"
            };

            var result = await _controller.UpdateProfile(request);

            Assert.IsType<OkObjectResult>(result);

            var organiser = await _context.Organisers
                .FirstAsync(o => o.OrganiserID == 1);

            Assert.Equal("Jane", organiser.FirstName);
            Assert.Equal("jane@example.com", organiser.Email);
        }

        [Fact]
        public async Task UpdateProfile_WithMissingRequiredDetails_ReturnsBadRequest()
        {
            await AddOrganiser();
            LoginAsOrganiser();

            var request = new UpdateProfileRequest
            {
                FirstName = "",
                LastName = "Jones",
                Email = "mary@example.com",
                Phone = "0712345678"
            };

            var result = await _controller.UpdateProfile(request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateProfile_WhenOrganiserDoesNotExist_ReturnsNotFound()
        {
            LoginAsOrganiser();

            var request = new UpdateProfileRequest
            {
                FirstName = "Jane",
                LastName = "Jones",
                Email = "jane@example.com",
                Phone = "0722222222"
            };

            var result = await _controller.UpdateProfile(request);

            Assert.IsType<NotFoundObjectResult>(result);
        }
    }
}

