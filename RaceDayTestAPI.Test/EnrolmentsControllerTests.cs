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

    public class EnrolmentsControllerTests : IDisposable
    {
        private readonly RaceDayTestDbContext _context;
        private readonly EnrolmentsController _controller;
        private readonly TestSession _session;

        public EnrolmentsControllerTests()
        {
            var options = new DbContextOptionsBuilder<RaceDayTestDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new RaceDayTestDbContext(options);

            _session = new TestSession();

            var httpContext = new DefaultHttpContext();
            httpContext.Session = _session;

            _controller = new EnrolmentsController(_context)
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

        private Event SeedEvent(
            DateTime? eventDate = null,
            int organiserId = 1,
            string name = "Test Event")
        {
            var ev = new Event
            {
                EventName = name,
                OrganiserID = organiserId,
                EventDate = eventDate ?? DateTime.Now.AddDays(30)
            };
            _context.Events.Add(ev);
            _context.SaveChanges();
            return ev;
        }

        private Category SeedCategory(int eventId, string name = "Senior")
        {
            var cat = new Category
            {
                EventID = eventId,
                CategoryName = name,
                CategoryType = "Age",
                MinAge = 18,
                MaxAge = 40,
                Description = "desc"
            };
            _context.Categories.Add(cat);
            _context.SaveChanges();
            return cat;
        }

        private Enrolment SeedEnrolment(
            int participantId,
            int eventId,
            int categoryId,
            string status = "Enrolled")
        {
            var enr = new Enrolment
            {
                ParticipantID = participantId,
                EventID = eventId,
                CategoryID = categoryId,
                EnrolmentDate = DateTime.Now,
                Status = status
            };
            _context.Enrolments.Add(enr);
            _context.SaveChanges();
            return enr;
        }

        private void LoginAsParticipant(int participantId)
        {
            _session.SetInt32("ParticipantID", participantId);
            _session.SetString("Role", "Participant");
        }

        // ============================================================
        // POST /api/events/{eventId}/enrolments
        // ============================================================

        [Fact]
        public async Task CreateEnrolment_ReturnsUnauthorized_WhenNotLoggedIn()
        {
            var ev = SeedEvent();
            var cat = SeedCategory(ev.EventID);
            var request = new EnrolmentRequest { CategoryID = cat.CategoryID };

            var result = await _controller.CreateEnrolment(ev.EventID, request);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task CreateEnrolment_ReturnsUnauthorized_WhenRoleIsOrganiser()
        {
            _session.SetInt32("ParticipantID", 1);
            _session.SetString("Role", "Organiser");

            var ev = SeedEvent();
            var cat = SeedCategory(ev.EventID);
            var request = new EnrolmentRequest { CategoryID = cat.CategoryID };

            var result = await _controller.CreateEnrolment(ev.EventID, request);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task CreateEnrolment_ReturnsNotFound_WhenEventDoesNotExist()
        {
            LoginAsParticipant(1);
            var request = new EnrolmentRequest { CategoryID = 1 };

            var result = await _controller.CreateEnrolment(999, request);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task CreateEnrolment_ReturnsBadRequest_WhenEventAlreadyHappened()
        {
            LoginAsParticipant(1);
            var ev = SeedEvent(eventDate: DateTime.Now.AddDays(-1));
            var cat = SeedCategory(ev.EventID);
            var request = new EnrolmentRequest { CategoryID = cat.CategoryID };

            var result = await _controller.CreateEnrolment(ev.EventID, request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CreateEnrolment_ReturnsBadRequest_WhenEventIsToday()
        {
            LoginAsParticipant(1);
            // Same day but earlier in the day (or "now") triggers the check
            var ev = SeedEvent(eventDate: DateTime.Now);
            var cat = SeedCategory(ev.EventID);
            var request = new EnrolmentRequest { CategoryID = cat.CategoryID };

            var result = await _controller.CreateEnrolment(ev.EventID, request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CreateEnrolment_ReturnsBadRequest_WhenCategoryDoesNotExist()
        {
            LoginAsParticipant(1);
            var ev = SeedEvent();
            var request = new EnrolmentRequest { CategoryID = 999 };

            var result = await _controller.CreateEnrolment(ev.EventID, request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CreateEnrolment_ReturnsBadRequest_WhenCategoryBelongsToAnotherEvent()
        {
            LoginAsParticipant(1);
            var ev1 = SeedEvent(name: "Event A");
            var ev2 = SeedEvent(name: "Event B");
            var otherCat = SeedCategory(ev2.EventID, "Other");

            var request = new EnrolmentRequest { CategoryID = otherCat.CategoryID };

            var result = await _controller.CreateEnrolment(ev1.EventID, request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CreateEnrolment_ReturnsConflict_WhenAlreadyEnrolled()
        {
            LoginAsParticipant(1);
            var ev = SeedEvent();
            var cat = SeedCategory(ev.EventID);
            SeedEnrolment(participantId: 1, eventId: ev.EventID, categoryId: cat.CategoryID);

            var request = new EnrolmentRequest { CategoryID = cat.CategoryID };

            var result = await _controller.CreateEnrolment(ev.EventID, request);

            Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task CreateEnrolment_Returns201_WhenValid()
        {
            LoginAsParticipant(42);
            var ev = SeedEvent();
            var cat = SeedCategory(ev.EventID);

            var request = new EnrolmentRequest { CategoryID = cat.CategoryID };

            var result = await _controller.CreateEnrolment(ev.EventID, request);

            var created = Assert.IsType<ObjectResult>(result);
            Assert.Equal(201, created.StatusCode);

            var stored = await _context.Enrolments.SingleAsync();
            Assert.Equal(42, stored.ParticipantID);
            Assert.Equal(ev.EventID, stored.EventID);
            Assert.Equal(cat.CategoryID, stored.CategoryID);
            Assert.Equal("Enrolled", stored.Status);
            Assert.True(stored.EnrolmentDate <= DateTime.Now);
        }

        [Fact]
        public async Task CreateEnrolment_AllowsDifferentParticipants_OnSameEvent()
        {
            var ev = SeedEvent();
            var cat = SeedCategory(ev.EventID);

            LoginAsParticipant(1);
            var r1 = await _controller.CreateEnrolment(ev.EventID,
                new EnrolmentRequest { CategoryID = cat.CategoryID });

            LoginAsParticipant(2);
            var r2 = await _controller.CreateEnrolment(ev.EventID,
                new EnrolmentRequest { CategoryID = cat.CategoryID });

            Assert.Equal(201, ((ObjectResult)r1).StatusCode);
            Assert.Equal(201, ((ObjectResult)r2).StatusCode);
            Assert.Equal(2, await _context.Enrolments.CountAsync());
        }

        [Fact]
        public async Task CreateEnrolment_ReturnsConflict_WhenCancelledEnrolmentExists()
        {
            // Current logic treats ANY enrolment (even cancelled) as already enrolled.
            // This test documents that behaviour.
            LoginAsParticipant(1);
            var ev = SeedEvent();
            var cat = SeedCategory(ev.EventID);
            SeedEnrolment(1, ev.EventID, cat.CategoryID, status: "Cancelled");

            var result = await _controller.CreateEnrolment(ev.EventID,
                new EnrolmentRequest { CategoryID = cat.CategoryID });

            Assert.IsType<ConflictObjectResult>(result);
        }

        // ============================================================
        // DELETE /api/Enrolments/{id}
        // ============================================================

        [Fact]
        public async Task CancelEnrolment_ReturnsUnauthorized_WhenNotLoggedIn()
        {
            var ev = SeedEvent();
            var cat = SeedCategory(ev.EventID);
            var enr = SeedEnrolment(1, ev.EventID, cat.CategoryID);

            var result = await _controller.CancelEnrolment(enr.EnrolmentID);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task CancelEnrolment_ReturnsUnauthorized_WhenRoleNotParticipant()
        {
            _session.SetInt32("ParticipantID", 1);
            _session.SetString("Role", "Organiser");

            var ev = SeedEvent();
            var cat = SeedCategory(ev.EventID);
            var enr = SeedEnrolment(1, ev.EventID, cat.CategoryID);

            var result = await _controller.CancelEnrolment(enr.EnrolmentID);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task CancelEnrolment_ReturnsNotFound_WhenEnrolmentMissing()
        {
            LoginAsParticipant(1);

            var result = await _controller.CancelEnrolment(999);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task CancelEnrolment_ReturnsNotFound_WhenEnrolmentBelongsToOtherParticipant()
        {
            var ev = SeedEvent();
            var cat = SeedCategory(ev.EventID);
            var enr = SeedEnrolment(participantId: 2, eventId: ev.EventID, categoryId: cat.CategoryID);

            LoginAsParticipant(1); // different participant

            var result = await _controller.CancelEnrolment(enr.EnrolmentID);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task CancelEnrolment_ReturnsBadRequest_WhenAlreadyCancelled()
        {
            LoginAsParticipant(1);
            var ev = SeedEvent();
            var cat = SeedCategory(ev.EventID);
            var enr = SeedEnrolment(1, ev.EventID, cat.CategoryID, status: "Cancelled");

            var result = await _controller.CancelEnrolment(enr.EnrolmentID);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CancelEnrolment_ReturnsNoContent_WhenSuccessful()
        {
            LoginAsParticipant(1);
            var ev = SeedEvent();
            var cat = SeedCategory(ev.EventID);
            var enr = SeedEnrolment(1, ev.EventID, cat.CategoryID);

            var result = await _controller.CancelEnrolment(enr.EnrolmentID);

            Assert.IsType<NoContentResult>(result);

            var updated = await _context.Enrolments.FindAsync(enr.EnrolmentID);
            Assert.NotNull(updated);
            Assert.Equal("Cancelled", updated!.Status);
        }

        [Fact]
        public async Task CancelEnrolment_DoesNotAffectOtherParticipantsEnrolments()
        {
            var ev = SeedEvent();
            var cat = SeedCategory(ev.EventID);
            var mine = SeedEnrolment(1, ev.EventID, cat.CategoryID);
            var theirs = SeedEnrolment(2, ev.EventID, cat.CategoryID);

            LoginAsParticipant(1);
            await _controller.CancelEnrolment(mine.EnrolmentID);

            var other = await _context.Enrolments.FindAsync(theirs.EnrolmentID);
            Assert.Equal("Enrolled", other!.Status);
        }
    }
}
