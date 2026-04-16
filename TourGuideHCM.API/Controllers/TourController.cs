using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourGuideHCM.API.Data;
using TourGuideHCM.API.Models;
using System.Text.Json;

namespace TourGuideHCM.API.Controllers;

[ApiController]
[Route("api/tour")]
public class TourController : ControllerBase
{
    private readonly AppDbContext _context;

    public TourController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var tours = await _context.Tours
            .OrderByDescending(t => t.Id)
            .ToListAsync();

        var result = tours.Select(t => new TourDto
        {
            Id = t.Id,
            Name = t.Name,
            Description = t.Description ?? "",
            Stops = string.IsNullOrEmpty(t.StopsJson)
                ? new()
                : JsonSerializer.Deserialize<List<TourStopDto>>(t.StopsJson) ?? new()
        }).ToList();

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var t = await _context.Tours.FindAsync(id);
        if (t == null) return NotFound();

        return Ok(new TourDto
        {
            Id = t.Id,
            Name = t.Name,
            Description = t.Description ?? "",
            Stops = string.IsNullOrEmpty(t.StopsJson)
                ? new()
                : JsonSerializer.Deserialize<List<TourStopDto>>(t.StopsJson) ?? new()
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TourDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("Vui lòng nhập tên tour");

        var tour = new Tour
        {
            Name = dto.Name,
            Description = dto.Description,
            StopsJson = JsonSerializer.Serialize(dto.Stops),
            CreatedAt = DateTime.UtcNow
        };
        _context.Tours.Add(tour);
        await _context.SaveChangesAsync();

        dto.Id = tour.Id;
        return Ok(dto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] TourDto dto)
    {
        var tour = await _context.Tours.FindAsync(id);
        if (tour == null) return NotFound();

        tour.Name = dto.Name;
        tour.Description = dto.Description;
        tour.StopsJson = JsonSerializer.Serialize(dto.Stops);
        tour.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(dto);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var tour = await _context.Tours.FindAsync(id);
        if (tour == null) return NotFound();
        _context.Tours.Remove(tour);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Đã xóa tour" });
    }
}

public class TourDto { public int Id { get; set; } public string Name { get; set; } = ""; public string Description { get; set; } = ""; public List<TourStopDto> Stops { get; set; } = new(); }
public class TourStopDto { public int Order { get; set; } public int PoiId { get; set; } public string PoiName { get; set; } = ""; public int DurationMinutes { get; set; } = 15; }
