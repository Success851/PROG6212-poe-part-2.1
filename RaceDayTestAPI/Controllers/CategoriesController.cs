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
    public class CategoriesController : ControllerBase
    {
        private readonly RaceDayTestDbContext _context;

        public CategoriesController(RaceDayTestDbContext context)
        {
            _context = context;
        }

        // GET: api/Categories/event/5
        [HttpGet("/api/Events/{eventId}/categories")]
        public async Task<IActionResult> GetEventCategories(int eventId)
        {
            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == eventId);

            if (eventItem == null)
                return NotFound("Event not found.");

            var categories = await _context.Categories
                .Where(c => c.EventID == eventId)
                .Select(c => new
                {
                    c.CategoryID,
                    c.EventID,
                    c.CategoryName,
                    c.CategoryType,
                    c.MinAge,
                    c.MaxAge,
                    c.DistanceKm,
                    c.Description
                })
                .ToListAsync();

            return Ok(categories);
        }

        // POST: api/Events/5/categories
        [HttpPost("/api/Events/{eventId}/categories")]
        public async Task<IActionResult> CreateCategory(
            int eventId,
            CreateCategoryRequest request)
        {
            var organiserID = HttpContext.Session.GetInt32("OrganiserID");
            var role = HttpContext.Session.GetString("Role");

            if (organiserID == null || role != "Organiser")
                return Unauthorized("Please log in as an organiser.");

            var eventItem = await _context.Events
                .FirstOrDefaultAsync(e => e.EventID == eventId);

            if (eventItem == null)
                return NotFound("Event not found.");

            if (eventItem.OrganiserID != organiserID.Value)
                return Forbid("You can only add categories to your own events.");

            if (string.IsNullOrWhiteSpace(request.CategoryName))
                return BadRequest("Category name is required.");

            if (string.IsNullOrWhiteSpace(request.CategoryType))
                return BadRequest("Category type is required.");

            string categoryType = request.CategoryType.Trim();

            if (categoryType != "Age" && categoryType != "Distance")
                return BadRequest("Category type must be Age or Distance.");

            if (categoryType == "Age")
            {
                if (request.MinAge == null || request.MaxAge == null)
                    return BadRequest("MinAge and MaxAge are required for Age categories.");

                if (request.MinAge < 0 || request.MaxAge < 0)
                    return BadRequest("Age cannot be negative.");

                if (request.MinAge > request.MaxAge)
                    return BadRequest("MinAge cannot be greater than MaxAge.");
            }

            if (categoryType == "Distance")
            {
                if (request.DistanceKm == null || request.DistanceKm <= 0)
                    return BadRequest("DistanceKm must be greater than zero for Distance categories.");
            }

            bool categoryExists = await _context.Categories
                .AnyAsync(c =>
                    c.EventID == eventId &&
                    c.CategoryName == request.CategoryName.Trim());

            if (categoryExists)
                return Conflict("A category with this name already exists for this event.");

            var category = new Category
            {
                EventID = eventId,
                CategoryName = request.CategoryName.Trim(),
                CategoryType = categoryType,
                MinAge = categoryType == "Age" ? request.MinAge : null,
                MaxAge = categoryType == "Age" ? request.MaxAge : null,
                DistanceKm = categoryType == "Distance" ? request.DistanceKm : null,
                Description = request.Description.Trim()
            };

            _context.Categories.Add(category);

            await _context.SaveChangesAsync();

            return StatusCode(201, new
            {
                message = "Category created successfully.",
                categoryID = category.CategoryID
            });
        }

        // PUT: api/Categories/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(
            int id,
            UpdateCategoryRequest request)
        {
            var organiserID = HttpContext.Session.GetInt32("OrganiserID");
            var role = HttpContext.Session.GetString("Role");

            if (organiserID == null || role != "Organiser")
                return Unauthorized("Please log in as an organiser.");

            var category = await _context.Categories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryID == id);

            if (category == null)
                return NotFound("Category not found.");

            if (category.Event.OrganiserID != organiserID.Value)
                return Forbid("You can only update categories for your own events.");

            if (string.IsNullOrWhiteSpace(request.CategoryName))
                return BadRequest("Category name is required.");

            if (string.IsNullOrWhiteSpace(request.CategoryType))
                return BadRequest("Category type is required.");

            string categoryType = request.CategoryType.Trim();

            if (categoryType != "Age" && categoryType != "Distance")
                return BadRequest("Category type must be Age or Distance.");

            if (categoryType == "Age")
            {
                if (request.MinAge == null || request.MaxAge == null)
                    return BadRequest("MinAge and MaxAge are required for Age categories.");

                if (request.MinAge < 0 || request.MaxAge < 0)
                    return BadRequest("Age cannot be negative.");

                if (request.MinAge > request.MaxAge)
                    return BadRequest("MinAge cannot be greater than MaxAge.");
            }

            if (categoryType == "Distance")
            {
                if (request.DistanceKm == null || request.DistanceKm <= 0)
                    return BadRequest("DistanceKm must be greater than zero for Distance categories.");
            }

            bool duplicateCategory = await _context.Categories
                .AnyAsync(c =>
                    c.CategoryID != id &&
                    c.EventID == category.EventID &&
                    c.CategoryName == request.CategoryName.Trim());

            if (duplicateCategory)
                return Conflict("A category with this name already exists for this event.");

            category.CategoryName = request.CategoryName.Trim();
            category.CategoryType = categoryType;
            category.MinAge = categoryType == "Age" ? request.MinAge : null;
            category.MaxAge = categoryType == "Age" ? request.MaxAge : null;
            category.DistanceKm = categoryType == "Distance"
                ? request.DistanceKm
                : null;
            category.Description = request.Description.Trim();

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Category updated successfully."
            });
        }

        // DELETE: api/Categories/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var organiserID = HttpContext.Session.GetInt32("OrganiserID");
            var role = HttpContext.Session.GetString("Role");

            if (organiserID == null || role != "Organiser")
                return Unauthorized("Please log in as an organiser.");

            var category = await _context.Categories
                .Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryID == id);

            if (category == null)
                return NotFound("Category not found.");

            if (category.Event.OrganiserID != organiserID.Value)
                return Forbid("You can only delete categories for your own events.");

            bool hasEnrolments = await _context.Enrolments
                .AnyAsync(e => e.CategoryID == id);

            if (hasEnrolments)
                return Conflict(
                    "This category cannot be deleted because participants are already enrolled in it.");

            _context.Categories.Remove(category);

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
