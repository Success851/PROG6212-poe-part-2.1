using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayTestAPI.Controllers;
using RaceDayTestAPI.Data;
using RaceDayTestAPI.DTOs;
using RaceDayTestAPI.Models;
using RaceDayTestAPI.Test;
using Xunit;

namespace RaceDayTestAPI.Test
{
    public class CategoriesControllerTests : IDisposable
    {
        private readonly RaceDayTestDbContext _context;
        private readonly CategoriesController _controller;
        private readonly TestSession _session;

        public CategoriesControllerTests()
        {
            var options = new DbContextOptionsBuilder<RaceDayTestDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new RaceDayTestDbContext(options);

            _session = new TestSession();

            var httpContext = new DefaultHttpContext();
            httpContext.Session = _session;

            _controller = new CategoriesController(_context)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = httpContext
                }
            };
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        // -------------------- HELPERS --------------------

        private Event SeedEvent(int organiserId = 1, string name = "Test Event")
        {
            var ev = new Event
            {
                EventID = 0,
                EventName = name,
                OrganiserID = organiserId
            };
            _context.Events.Add(ev);
            _context.SaveChanges();
            return ev;
        }

        private Category SeedCategory(int eventId, string name = "Senior",
            string type = "Age", int? minAge = 18, int? maxAge = 40)
        {
            var cat = new Category
            {
                EventID = eventId,
                CategoryName = name,
                CategoryType = type,
                MinAge = minAge,
                MaxAge = maxAge,
                DistanceKm = type == "Distance" ? 10 : null,
                Description = "desc"
            };
            _context.Categories.Add(cat);
            _context.SaveChanges();
            return cat;
        }

        private void LoginAsOrganiser(int organiserId)
        {
            _session.SetInt32("OrganiserID", organiserId);
            _session.SetString("Role", "Organiser");
        }

        // ============================================================
        // GET /api/Events/{eventId}/categories
        // ============================================================

        [Fact]
        public async Task GetEventCategories_ReturnsNotFound_WhenEventDoesNotExist()
        {
            var result = await _controller.GetEventCategories(999);
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetEventCategories_ReturnsEmptyList_WhenNoCategories()
        {
            var ev = SeedEvent();

            var result = await _controller.GetEventCategories(ev.EventID);

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<System.Collections.IEnumerable>(ok.Value);
            Assert.Empty(list.Cast<object>());
        }

        [Fact]
        public async Task GetEventCategories_ReturnsCategories_ForEvent()
        {
            var ev = SeedEvent();
            SeedCategory(ev.EventID, "Senior");
            SeedCategory(ev.EventID, "Junior", minAge: 10, maxAge: 17);
            SeedCategory(ev.EventID + 1, "Other"); // different event

            var result = await _controller.GetEventCategories(ev.EventID);

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = ((System.Collections.IEnumerable)ok.Value!).Cast<object>().ToList();
            Assert.Equal(2, list.Count);
        }

        // ============================================================
        // POST /api/Events/{eventId}/categories
        // ============================================================

        [Fact]
        public async Task CreateCategory_ReturnsUnauthorized_WhenNotLoggedIn()
        {
            var ev = SeedEvent();
            var req = new CreateCategoryRequest
            {
                CategoryName = "Senior",
                CategoryType = "Age",
                MinAge = 18,
                MaxAge = 40,
                Description = "d"
            };

            var result = await _controller.CreateCategory(ev.EventID, req);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task CreateCategory_ReturnsUnauthorized_WhenRoleNotOrganiser()
        {
            _session.SetInt32("OrganiserID", 1);
            _session.SetString("Role", "Participant");

            var ev = SeedEvent();
            var req = new CreateCategoryRequest
            {
                CategoryName = "Senior",
                CategoryType = "Age",
                MinAge = 18,
                MaxAge = 40,
                Description = "d"
            };

            var result = await _controller.CreateCategory(ev.EventID, req);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task CreateCategory_ReturnsNotFound_WhenEventMissing()
        {
            LoginAsOrganiser(1);
            var req = new CreateCategoryRequest
            {
                CategoryName = "Senior",
                CategoryType = "Age",
                MinAge = 18,
                MaxAge = 40,
                Description = "d"
            };

            var result = await _controller.CreateCategory(999, req);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task CreateCategory_ReturnsForbid_WhenEventBelongsToAnotherOrganiser()
        {
            LoginAsOrganiser(1);
            var ev = SeedEvent(organiserId: 2);
            var req = new CreateCategoryRequest
            {
                CategoryName = "Senior",
                CategoryType = "Age",
                MinAge = 18,
                MaxAge = 40,
                Description = "d"
            };

            var result = await _controller.CreateCategory(ev.EventID, req);

            Assert.IsType<ForbidResult>(result);
        }

        [Theory]
        [InlineData("", "Age")]
        [InlineData("   ", "Age")]
        [InlineData("Senior", "")]
        [InlineData("Senior", "InvalidType")]
        public async Task CreateCategory_ReturnsBadRequest_ForInvalidNameOrType(
            string name, string type)
        {
            LoginAsOrganiser(1);
            var ev = SeedEvent(1);
            var req = new CreateCategoryRequest
            {
                CategoryName = name,
                CategoryType = type,
                MinAge = 18,
                MaxAge = 40,
                Description = "d"
            };

            var result = await _controller.CreateCategory(ev.EventID, req);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CreateCategory_ReturnsBadRequest_WhenAgeFieldsMissing()
        {
            LoginAsOrganiser(1);
            var ev = SeedEvent(1);
            var req = new CreateCategoryRequest
            {
                CategoryName = "Senior",
                CategoryType = "Age",
                MinAge = null,
                MaxAge = null,
                Description = "d"
            };

            var result = await _controller.CreateCategory(ev.EventID, req);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CreateCategory_ReturnsBadRequest_WhenMinAgeGreaterThanMaxAge()
        {
            LoginAsOrganiser(1);
            var ev = SeedEvent(1);
            var req = new CreateCategoryRequest
            {
                CategoryName = "Senior",
                CategoryType = "Age",
                MinAge = 50,
                MaxAge = 20,
                Description = "d"
            };

            var result = await _controller.CreateCategory(ev.EventID, req);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CreateCategory_ReturnsBadRequest_WhenDistanceMissing()
        {
            LoginAsOrganiser(1);
            var ev = SeedEvent(1);
            var req = new CreateCategoryRequest
            {
                CategoryName = "10K",
                CategoryType = "Distance",
                DistanceKm = 0,
                Description = "d"
            };

            var result = await _controller.CreateCategory(ev.EventID, req);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CreateCategory_ReturnsConflict_WhenDuplicateName()
        {
            LoginAsOrganiser(1);
            var ev = SeedEvent(1);
            SeedCategory(ev.EventID, "Senior");

            var req = new CreateCategoryRequest
            {
                CategoryName = "Senior",
                CategoryType = "Age",
                MinAge = 18,
                MaxAge = 40,
                Description = "d"
            };

            var result = await _controller.CreateCategory(ev.EventID, req);

            Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task CreateCategory_Returns201_ForValidAgeCategory()
        {
            LoginAsOrganiser(1);
            var ev = SeedEvent(1);
            var req = new CreateCategoryRequest
            {
                CategoryName = "Senior",
                CategoryType = "Age",
                MinAge = 18,
                MaxAge = 40,
                Description = "desc"
            };

            var result = await _controller.CreateCategory(ev.EventID, req);

            var created = Assert.IsType<ObjectResult>(result);
            Assert.Equal(201, created.StatusCode);

            var stored = await _context.Categories
                .SingleAsync(c => c.EventID == ev.EventID);
            Assert.Equal("Senior", stored.CategoryName);
            Assert.Equal(18, stored.MinAge);
            Assert.Equal(40, stored.MaxAge);
            Assert.Null(stored.DistanceKm);
        }

        [Fact]
        public async Task CreateCategory_Returns201_ForValidDistanceCategory()
        {
            LoginAsOrganiser(1);
            var ev = SeedEvent(1);
            var req = new CreateCategoryRequest
            {
                CategoryName = "10K",
                CategoryType = "Distance",
                DistanceKm = 10,
                Description = "10 kilometre"
            };

            var result = await _controller.CreateCategory(ev.EventID, req);

            var created = Assert.IsType<ObjectResult>(result);
            Assert.Equal(201, created.StatusCode);

            var stored = await _context.Categories.SingleAsync();
            Assert.Equal(10, stored.DistanceKm);
            Assert.Null(stored.MinAge);
            Assert.Null(stored.MaxAge);
        }

        // ============================================================
        // PUT /api/Categories/{id}
        // ============================================================

        [Fact]
        public async Task UpdateCategory_ReturnsUnauthorized_WhenNotLoggedIn()
        {
            var ev = SeedEvent();
            var cat = SeedCategory(ev.EventID);
            var req = new UpdateCategoryRequest
            {
                CategoryName = "X",
                CategoryType = "Age",
                MinAge = 1,
                MaxAge = 2,
                Description = "d"
            };

            var result = await _controller.UpdateCategory(cat.CategoryID, req);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task UpdateCategory_ReturnsNotFound_WhenCategoryMissing()
        {
            LoginAsOrganiser(1);
            var req = new UpdateCategoryRequest
            {
                CategoryName = "X",
                CategoryType = "Age",
                MinAge = 1,
                MaxAge = 2,
                Description = "d"
            };

            var result = await _controller.UpdateCategory(999, req);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task UpdateCategory_ReturnsForbid_WhenNotOwner()
        {
            var ev = SeedEvent(organiserId: 2);
            var cat = SeedCategory(ev.EventID);
            LoginAsOrganiser(1);

            var req = new UpdateCategoryRequest
            {
                CategoryName = "Updated",
                CategoryType = "Age",
                MinAge = 18,
                MaxAge = 40,
                Description = "d"
            };

            var result = await _controller.UpdateCategory(cat.CategoryID, req);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task UpdateCategory_ReturnsConflict_WhenDuplicateName()
        {
            LoginAsOrganiser(1);
            var ev = SeedEvent(1);
            SeedCategory(ev.EventID, "Senior");
            var junior = SeedCategory(ev.EventID, "Junior", minAge: 10, maxAge: 17);

            var req = new UpdateCategoryRequest
            {
                CategoryName = "Senior",
                CategoryType = "Age",
                MinAge = 10,
                MaxAge = 17,
                Description = "d"
            };

            var result = await _controller.UpdateCategory(junior.CategoryID, req);

            Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task UpdateCategory_UpdatesFields_Correctly()
        {
            LoginAsOrganiser(1);
            var ev = SeedEvent(1);
            var cat = SeedCategory(ev.EventID, "Senior", minAge: 18, maxAge: 40);

            var req = new UpdateCategoryRequest
            {
                CategoryName = "Veteran",
                CategoryType = "Distance",
                DistanceKm = 21,
                Description = "Half marathon"
            };

            var result = await _controller.UpdateCategory(cat.CategoryID, req);

            Assert.IsType<OkObjectResult>(result);

            var updated = await _context.Categories.FindAsync(cat.CategoryID);
            Assert.NotNull(updated);
            Assert.Equal("Veteran", updated!.CategoryName);
            Assert.Equal("Distance", updated.CategoryType);
            Assert.Equal(21, updated.DistanceKm);
            Assert.Null(updated.MinAge);
            Assert.Null(updated.MaxAge);
        }

        [Fact]
        public async Task UpdateCategory_ReturnsBadRequest_WhenChangingToAgeWithoutAgeFields()
        {
            LoginAsOrganiser(1);
            var ev = SeedEvent(1);
            var cat = SeedCategory(ev.EventID, "10K", "Distance");

            var req = new UpdateCategoryRequest
            {
                CategoryName = "10K",
                CategoryType = "Age",
                MinAge = null,
                MaxAge = null,
                Description = "d"
            };

            var result = await _controller.UpdateCategory(cat.CategoryID, req);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ============================================================
        // DELETE /api/Categories/{id}
        // ============================================================

        [Fact]
        public async Task DeleteCategory_ReturnsUnauthorized_WhenNotLoggedIn()
        {
            var ev = SeedEvent();
            var cat = SeedCategory(ev.EventID);

            var result = await _controller.DeleteCategory(cat.CategoryID);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task DeleteCategory_ReturnsNotFound_WhenMissing()
        {
            LoginAsOrganiser(1);

            var result = await _controller.DeleteCategory(999);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task DeleteCategory_ReturnsForbid_WhenNotOwner()
        {
            var ev = SeedEvent(2);
            var cat = SeedCategory(ev.EventID);
            LoginAsOrganiser(1);

            var result = await _controller.DeleteCategory(cat.CategoryID);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task DeleteCategory_ReturnsConflict_WhenEnrolmentsExist()
        {
            LoginAsOrganiser(1);
            var ev = SeedEvent(1);
            var cat = SeedCategory(ev.EventID);

            _context.Enrolments.Add(new Enrolment
            {
                CategoryID = cat.CategoryID,
                // populate any other required fields for Enrolment
            });
            await _context.SaveChangesAsync();

            var result = await _controller.DeleteCategory(cat.CategoryID);

            Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task DeleteCategory_ReturnsNoContent_WhenSuccessful()
        {
            LoginAsOrganiser(1);
            var ev = SeedEvent(1);
            var cat = SeedCategory(ev.EventID);

            var result = await _controller.DeleteCategory(cat.CategoryID);

            Assert.IsType<NoContentResult>(result);
            Assert.Null(await _context.Categories.FindAsync(cat.CategoryID));
        }
    }
}
