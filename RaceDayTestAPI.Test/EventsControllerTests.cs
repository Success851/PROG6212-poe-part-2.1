using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using RaceDayTestAPI.Controllers;
using RaceDayTestAPI.Data;
using RaceDayTestAPI.DTOs;
using RaceDayTestAPI.Models;

namespace RaceDayTestAPI.Test
{
    public class EventsControllerTests
    {
        private RaceDayTestDbContext CreateDatabase()
        {
            var options = new DbContextOptionsBuilder<RaceDayTestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new RaceDayTestDbContext(options);
        }

        [Fact]
        public async Task RegisterParticipant_Returns201()
        {
            // Arrange
            using var context = CreateDatabase();

            var controller = new AuthController(context);

            var request = new RegisterRequest
            {
                FirstName = "John",
                LastName = "Mokoena",
                Email = "john@test.com",
                Password = "Password123!",
                Phone = "0712345678",
                DateOfBirth = new DateTime(2005, 5, 10),
                Role = "Participant"
            };

            // Act
            var result = await controller.Register(request);

            // Assert
            var response = Assert.IsType<ObjectResult>(result);

            Assert.Equal(201, response.StatusCode);

            var participant = await context.Participants
                .FirstOrDefaultAsync(p => p.Email == "john@test.com");

            Assert.NotNull(participant);
            Assert.Equal("John", participant.FirstName);
            Assert.Equal("Mokoena", participant.LastName);
        }

        [Fact]
        public async Task RegisterOrganiser_Returns201()
        {
            // Arrange
            using var context = CreateDatabase();

            var controller = new AuthController(context);

            var request = new RegisterRequest
            {
                FirstName = "Peter",
                LastName = "Dlamini",
                Email = "peter@test.com",
                Password = "Password123!",
                Phone = "0723456789",
                Role = "Organiser"
            };

            // Act
            var result = await controller.Register(request);

            // Assert
            var response = Assert.IsType<ObjectResult>(result);

            Assert.Equal(201, response.StatusCode);

            var organiser = await context.Organisers
                .FirstOrDefaultAsync(o => o.Email == "peter@test.com");

            Assert.NotNull(organiser);
            Assert.Equal("Peter", organiser.FirstName);
            Assert.Equal("Dlamini", organiser.LastName);
        }

        [Fact]
        public async Task RegisterWithDuplicateEmail_Returns409()
        {
            // Arrange
            using var context = CreateDatabase();

            var controller = new AuthController(context);

            var firstRequest = new RegisterRequest
            {
                FirstName = "John",
                LastName = "Mokoena",
                Email = "john@test.com",
                Password = "Password123!",
                Phone = "0712345678",
                Role = "Participant"
            };

            await controller.Register(firstRequest);

            var secondRequest = new RegisterRequest
            {
                FirstName = "Peter",
                LastName = "Dlamini",
                Email = "john@test.com",
                Password = "Password123!",
                Phone = "0723456789",
                Role = "Organiser"
            };

            // Act
            var result = await controller.Register(secondRequest);

            // Assert
            var response = Assert.IsType<ConflictObjectResult>(result);

            Assert.Equal(409, response.StatusCode);
        }

        [Fact]
        public async Task RegisterWithInvalidRole_Returns400()
        {
            // Arrange
            using var context = CreateDatabase();

            var controller = new AuthController(context);

            var request = new RegisterRequest
            {
                FirstName = "John",
                LastName = "Mokoena",
                Email = "invalid@test.com",
                Password = "Password123!",
                Phone = "0712345678",
                Role = "Admin"
            };

            // Act
            var result = await controller.Register(request);

            // Assert
            var response = Assert.IsType<BadRequestObjectResult>(result);

            Assert.Equal(400, response.StatusCode);
        }

        [Fact]
        public async Task RegisterWithoutRequiredFields_Returns400()
        {
            // Arrange
            using var context = CreateDatabase();

            var controller = new AuthController(context);

            var request = new RegisterRequest
            {
                FirstName = "",
                LastName = "",
                Email = "",
                Password = "",
                Phone = "",
                Role = ""
            };

            // Act
            var result = await controller.Register(request);

            // Assert
            var response = Assert.IsType<BadRequestObjectResult>(result);

            Assert.Equal(400, response.StatusCode);
        }
    }
}