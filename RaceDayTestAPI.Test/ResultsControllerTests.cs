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

    public class ResultsControllerTests : IDisposable
    {
        private readonly RaceDayTestDbContext _context;
        private readonly ResultsController _controller;
        private readonly TestSession _session;

        public ResultsControllerTests()
        {
            var options = new DbContextOptionsBuilder<RaceDayTestDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new RaceDayTestDbContext(options);

            _session = new TestSession();

            var httpContext = new DefaultHttpContext();
            httpContext.Session = _session;

            _controller = new ResultsController(_context)
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
            int organiserId = 1,
            string name = "Test Event",
            DateTime? date = null)
        {
            var ev = new Event
            {
                EventName = name,
                OrganiserID = organiserId,
                EventDate = date ?? DateTime.Now.AddDays(30),
                Location = "Test Location",
                EventType = "Road"
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

        private Participant SeedParticipant(
            int id = 0,
            string firstName = "John",
            string lastName = "Doe",
            string email = "john@test.com")
        {
            var p = new Participant
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email
            };
            _context.Participants.Add(p);
            _context.SaveChanges();
            return p;
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

        private Result SeedResult(
            int enrolmentId,
            string status = "Completed",
            int? position = 1,
            TimeSpan? finishTime = null)
        {
            var r = new Result
            {
                EnrolmentID = enrolmentId,
                ResultStatus = status,
                Position = position,
                FinishTime = finishTime ?? TimeSpan.FromMinutes(45),
                RecordedAt = DateTime.Now
            };
            _context.Results.Add(r);
            _context.SaveChanges();
            return r;
        }

        private void LoginAsOrganiser(int organiserId)
        {
            _session.SetInt32("OrganiserID", organiserId);
            _session.SetString("Role", "Organiser");
        }

        private void LoginAsParticipant(int participantId)
        {
            _session.SetInt32("ParticipantID", participantId);
            _session.SetString("Role", "Participant");
        }

        // ============================================================
        // POST /api/enrolments/{enrolmentId}/result
        // ============================================================

        [Fact]
        public async Task CreateResult_ReturnsUnauthorized_WhenNotLoggedIn()
        {
            var ev = SeedEvent();
            var cat = SeedCategory(ev.EventID);
            var p = SeedParticipant();
            var enr = SeedEnrolment(p.ParticipantID, ev.EventID, cat.CategoryID);

            var request = new CreateResultRequest
            {
                ResultStatus = "Completed",
                Position = 1,
                FinishTime = TimeSpan.FromMinutes(45)
            };

            var result = await _controller.CreateResult(enr.EnrolmentID, request);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task CreateResult_ReturnsUnauthorized_WhenRoleNotOrganiser()
        {
            _session.SetInt32("OrganiserID", 1);
            _session.SetString("Role", "Participant");

            var ev = SeedEvent();
            var cat = SeedCategory(ev.EventID);
            var p = SeedParticipant();
            var enr = SeedEnrolment(p.ParticipantID, ev.EventID, cat.CategoryID);

            var request = new CreateResultRequest
            {
                ResultStatus = "Completed",
                Position = 1,
                FinishTime = TimeSpan.FromMinutes(45)
            };

            var result = await _controller.CreateResult(enr.EnrolmentID, request);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task CreateResult_ReturnsNotFound_WhenEnrolmentMissing()
        {
            LoginAsOrganiser(1);

            var request = new CreateResultRequest
            {
                ResultStatus = "Completed",
                Position = 1,
                FinishTime = TimeSpan.FromMinutes(45)
            };

            var result = await _controller.CreateResult(999, request);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task CreateResult_ReturnsForbid_WhenOrganiserDoesNotOwnEvent()
        {
            var ev = SeedEvent(organiserId: 2);
            var cat = SeedCategory(ev.EventID);
            var p = SeedParticipant();
            var enr = SeedEnrolment(p.ParticipantID, ev.EventID, cat.CategoryID);

            LoginAsOrganiser(1);

            var request = new CreateResultRequest
            {
                ResultStatus = "Completed",
                Position = 1,
                FinishTime = TimeSpan.FromMinutes(45)
            };

            var result = await _controller.CreateResult(enr.EnrolmentID, request);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task CreateResult_ReturnsBadRequest_WhenEnrolmentNotActive()
        {
            LoginAsOrganiser(1);
            var ev = SeedEvent(organiserId: 1);
            var cat = SeedCategory(ev.EventID);
            var p = SeedParticipant();
            var enr = SeedEnrolment(p.ParticipantID, ev.EventID, cat.CategoryID,
                status: "Cancelled");

            var request = new CreateResultRequest
            {
                ResultStatus = "Completed",
                Position = 1,
                FinishTime = TimeSpan.FromMinutes(45)
            };

            var result = await _controller.CreateResult(enr.EnrolmentID, request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CreateResult_ReturnsConflict_WhenResultAlreadyExists()
        {
            LoginAsOrganiser(1);
            var ev = SeedEvent(organiserId: 1);
            var cat = SeedCategory(ev.EventID);
            var p = SeedParticipant();
            var enr = SeedEnrolment(p.ParticipantID, ev.EventID, cat.CategoryID);
            SeedResult(enr.EnrolmentID);

            var request = new CreateResultRequest
            {
                ResultStatus = "Completed",
                Position = 1,
                FinishTime = TimeSpan.FromMinutes(45)
            };

            var result = await _controller.CreateResult(enr.EnrolmentID, request);

            Assert.IsType<ConflictObjectResult>(result);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("Finished")]
        [InlineData("DNF")]
        [InlineData("invalid")]
        public async Task CreateResult_ReturnsBadRequest_ForInvalidStatus(string status)
        {
            LoginAsOrganiser(1);
            var ev = SeedEvent(organiserId: 1);
            var cat = SeedCategory(ev.EventID);
            var p = SeedParticipant();
            var enr = SeedEnrolment(p.ParticipantID, ev.EventID, cat.CategoryID);

            var request = new CreateResultRequest
            {
                ResultStatus = status,
                Position = 1,
                FinishTime = TimeSpan.FromMinutes(45)
            };

            var result = await _controller.CreateResult(enr.EnrolmentID, request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public async Task CreateResult_ReturnsBadRequest_ForInvalidPosition(int position)
        {
            LoginAsOrganiser(1);
            var ev = SeedEvent(organiserId: 1);
            var cat = SeedCategory(ev.EventID);
            var p = SeedParticipant();
            var enr = SeedEnrolment(p.ParticipantID, ev.EventID, cat.CategoryID);

            var request = new CreateResultRequest
            {
                ResultStatus = "Completed",
                Position = position,
                FinishTime = TimeSpan.FromMinutes(45)
            };

            var result = await _controller.CreateResult(enr.EnrolmentID, request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Theory]
        [InlineData("Completed")]
        [InlineData("Did Not Finish")]
        [InlineData("Disqualified")]
        public async Task CreateResult_Returns201_ForValidStatuses(string status)
        {
            LoginAsOrganiser(1);
            var ev = SeedEvent(organiserId: 1);
            var cat = SeedCategory(ev.EventID);
            var p = SeedParticipant();
            var enr = SeedEnrolment(p.ParticipantID, ev.EventID, cat.CategoryID);

            var request = new CreateResultRequest
            {
                ResultStatus = status,
                Position = 1,
                FinishTime = TimeSpan.FromMinutes(45)
            };

            var result = await _controller.CreateResult(enr.EnrolmentID, request);

            var created = Assert.IsType<ObjectResult>(result);
            Assert.Equal(201, created.StatusCode);

            var stored = await _context.Results.SingleAsync();
            Assert.Equal(status, stored.ResultStatus);
            Assert.Equal(1, stored.Position);
        }

        [Fact]
        public async Task CreateResult_Returns201_WhenPositionIsNull()
        {
            LoginAsOrganiser(1);
            var ev = SeedEvent(organiserId: 1);
            var cat = SeedCategory(ev.EventID);
            var p = SeedParticipant();
            var enr = SeedEnrolment(p.ParticipantID, ev.EventID, cat.CategoryID);

            var request = new CreateResultRequest
            {
                ResultStatus = "Did Not Finish",
                Position = null,
                FinishTime = null
            };

            var result = await _controller.CreateResult(enr.EnrolmentID, request);

            var created = Assert.IsType<ObjectResult>(result);
            Assert.Equal(201, created.StatusCode);
            Assert.Null((await _context.Results.SingleAsync()).Position);
        }

        [Fact]
        public async Task CreateResult_TrimsStatusWhitespace()
        {
            LoginAsOrganiser(1);
            var ev = SeedEvent(organiserId: 1);
            var cat = SeedCategory(ev.EventID);
            var p = SeedParticipant();
            var enr = SeedEnrolment(p.ParticipantID, ev.EventID, cat.CategoryID);

            var request = new CreateResultRequest
            {
                ResultStatus = "  Completed  ",
                Position = 1,
                FinishTime = TimeSpan.FromMinutes(45)
            };

            var result = await _controller.CreateResult(enr.EnrolmentID, request);

            Assert.Equal(201, ((ObjectResult)result).StatusCode);
            Assert.Equal("Completed", (await _context.Results.SingleAsync()).ResultStatus);
        }

        // ============================================================
        // PUT /api/enrolments/{enrolmentId}/result
        // ============================================================

        [Fact]
        public async Task UpdateResult_ReturnsUnauthorized_WhenNotLoggedIn()
        {
            var ev = SeedEvent();
            var cat = SeedCategory(ev.EventID);
            var p = SeedParticipant();
            var enr = SeedEnrolment(p.ParticipantID, ev.EventID, cat.CategoryID);
            SeedResult(enr.EnrolmentID);

            var request = new UpdateResultRequest
            {
                ResultStatus = "Completed",
                Position = 1,
                FinishTime = TimeSpan.FromMinutes(30)
            };

            var result = await _controller.UpdateResult(enr.EnrolmentID, request);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task UpdateResult_ReturnsNotFound_WhenResultMissing()
        {
            LoginAsOrganiser(1);

            var request = new UpdateResultRequest
            {
                ResultStatus = "Completed",
                Position = 1,
                FinishTime = TimeSpan.FromMinutes(30)
            };

            var result = await _controller.UpdateResult(999, request);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task UpdateResult_ReturnsForbid_WhenNotOwner()
        {
            var ev = SeedEvent(organiserId: 2);
            var cat = SeedCategory(ev.EventID);
            var p = SeedParticipant();
            var enr = SeedEnrolment(p.ParticipantID, ev.EventID, cat.CategoryID);
            SeedResult(enr.EnrolmentID);

            LoginAsOrganiser(1);

            var request = new UpdateResultRequest
            {
                ResultStatus = "Completed",
                Position = 1,
                FinishTime = TimeSpan.FromMinutes(30)
            };

            var result = await _controller.UpdateResult(enr.EnrolmentID, request);

            Assert.IsType<ForbidResult>(result);
        }

        [Theory]
        [InlineData("Bad")]
        [InlineData("")]
        [InlineData("DNF")]
        public async Task UpdateResult_ReturnsBadRequest_ForInvalidStatus(string status)
        {
            LoginAsOrganiser(1);
            var ev = SeedEvent(organiserId: 1);
            var cat = SeedCategory(ev.EventID);
            var p = SeedParticipant();
            var enr = SeedEnrolment(p.ParticipantID, ev.EventID, cat.CategoryID);
            SeedResult(enr.EnrolmentID);

            var request = new UpdateResultRequest
            {
                ResultStatus = status,
                Position = 1,
                FinishTime = TimeSpan.FromMinutes(30)
            };

            var result = await _controller.UpdateResult(enr.EnrolmentID, request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public async Task UpdateResult_ReturnsBadRequest_ForInvalidPosition(int pos)
        {
            LoginAsOrganiser(1);
            var ev = SeedEvent(organiserId: 1);
            var cat = SeedCategory(ev.EventID);
            var p = SeedParticipant();
            var enr = SeedEnrolment(p.ParticipantID, ev.EventID, cat.CategoryID);
            SeedResult(enr.EnrolmentID);

            var request = new UpdateResultRequest
            {
                ResultStatus = "Completed",
                Position = pos,
                FinishTime = TimeSpan.FromMinutes(30)
            };

            var result = await _controller.UpdateResult(enr.EnrolmentID, request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateResult_ReturnsOk_AndUpdatesFields()
        {
            LoginAsOrganiser(1);
            var ev = SeedEvent(organiserId: 1);
            var cat = SeedCategory(ev.EventID);
            var p = SeedParticipant();
            var enr = SeedEnrolment(p.ParticipantID, ev.EventID, cat.CategoryID);
            var res = SeedResult(enr.EnrolmentID,
                status: "Completed", position: 1,
                finishTime: TimeSpan.FromMinutes(45));

            var request = new UpdateResultRequest
            {
                ResultStatus = "Disqualified",
                Position = null,
                FinishTime = TimeSpan.FromMinutes(50)
            };

            var result = await _controller.UpdateResult(enr.EnrolmentID, request);

            Assert.IsType<OkObjectResult>(result);

            var updated = await _context.Results.FindAsync(res.ResultID);
            Assert.NotNull(updated);
            Assert.Equal("Disqualified", updated!.ResultStatus);
            Assert.Null(updated.Position);
            Assert.Equal(TimeSpan.FromMinutes(50), updated.FinishTime);
        }

        // ============================================================
        // GET /api/Results/my-results
        // ============================================================

        [Fact]
        public async Task GetMyResults_ReturnsUnauthorized_WhenNotLoggedIn()
        {
            var result = await _controller.GetMyResults();
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task GetMyResults_ReturnsUnauthorized_WhenRoleIsOrganiser()
        {
            _session.SetInt32("ParticipantID", 1);
            _session.SetString("Role", "Organiser");

            var result = await _controller.GetMyResults();

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task GetMyResults_ReturnsEmpty_WhenParticipantHasNoResults()
        {
            LoginAsParticipant(1);

            var result = await _controller.GetMyResults();

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = ((System.Collections.IEnumerable)ok.Value!).Cast<object>();
            Assert.Empty(list);
        }

        [Fact]
        public async Task GetMyResults_ReturnsOnlyOwnResults()
        {
            var ev = SeedEvent();
            var cat = SeedCategory(ev.EventID);
            var me = SeedParticipant(email: "me@test.com");
            var other = SeedParticipant(email: "other@test.com");

            var myEnr = SeedEnrolment(me.ParticipantID, ev.EventID, cat.CategoryID);
            var otherEnr = SeedEnrolment(other.ParticipantID, ev.EventID, cat.CategoryID);

            SeedResult(myEnr.EnrolmentID, position: 1);
            SeedResult(otherEnr.EnrolmentID, position: 2);

            LoginAsParticipant(me.ParticipantID);

            var result = await _controller.GetMyResults();

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = ((System.Collections.IEnumerable)ok.Value!).Cast<object>().ToList();
            Assert.Single(list);
        }

        // ============================================================
        // GET /api/events/{eventId}/results
        // ============================================================

        [Fact]
        public async Task GetEventResults_ReturnsUnauthorized_WhenNotLoggedIn()
        {
            var result = await _controller.GetEventResults(1);
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task GetEventResults_ReturnsNotFound_WhenEventMissing()
        {
            LoginAsOrganiser(1);

            var result = await _controller.GetEventResults(999);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetEventResults_ReturnsForbid_WhenNotOwner()
        {
            var ev = SeedEvent(organiserId: 2);
            LoginAsOrganiser(1);

            var result = await _controller.GetEventResults(ev.EventID);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task GetEventResults_ReturnsResults_ForEvent()
        {
            LoginAsOrganiser(1);
            var ev = SeedEvent(organiserId: 1);
            var cat = SeedCategory(ev.EventID);
            var p1 = SeedParticipant(email: "a@test.com", firstName: "Alice");
            var p2 = SeedParticipant(email: "b@test.com", firstName: "Bob");

            var e1 = SeedEnrolment(p1.ParticipantID, ev.EventID, cat.CategoryID);
            var e2 = SeedEnrolment(p2.ParticipantID, ev.EventID, cat.CategoryID);

            SeedResult(e1.EnrolmentID, position: 2);
            SeedResult(e2.EnrolmentID, position: 1);

            var result = await _controller.GetEventResults(ev.EventID);

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = ((System.Collections.IEnumerable)ok.Value!).Cast<object>().ToList();
            Assert.Equal(2, list.Count);
        }

        [Fact]
        public async Task GetEventResults_DoesNotReturnOtherEventsResults()
        {
            LoginAsOrganiser(1);
            var ev1 = SeedEvent(organiserId: 1, name: "Event A");
            var ev2 = SeedEvent(organiserId: 1, name: "Event B");
            var cat1 = SeedCategory(ev1.EventID);
            var cat2 = SeedCategory(ev2.EventID);
            var p = SeedParticipant();

            var e1 = SeedEnrolment(p.ParticipantID, ev1.EventID, cat1.CategoryID);
            var e2 = SeedEnrolment(p.ParticipantID, ev2.EventID, cat2.CategoryID);

            SeedResult(e1.EnrolmentID, position: 1);
            SeedResult(e2.EnrolmentID, position: 1);

            var result = await _controller.GetEventResults(ev1.EventID);

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = ((System.Collections.IEnumerable)ok.Value!).Cast<object>().ToList();
            Assert.Single(list);
        }
    }
}