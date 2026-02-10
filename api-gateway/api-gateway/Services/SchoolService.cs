using api_gateway.Data;
using api_gateway.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace api_gateway.Services;

public interface ISchoolService
{
    Task<School?> GetSchoolByIdAsync(int id);
    Task<List<School>> GetAllSchoolsAsync();
    Task<School> CreateSchoolAsync(School school);
    Task<School> UpdateSchoolAsync(School school);
    Task<bool> DeleteSchoolAsync(int id);
    Task<List<School>> GetSchoolsByCityAsync(string city);
}

public class SchoolService : ISchoolService
{
    private readonly ApiGatewayDbContext _context;
    private readonly ILogger<SchoolService> _logger;

    public SchoolService(ApiGatewayDbContext context, ILogger<SchoolService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<School?> GetSchoolByIdAsync(int id)
    {
        try
        {
            return await _context.Schools.FirstOrDefaultAsync(s => s.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting school by id: {Id}", id);
            return null;
        }
    }

    public async Task<List<School>> GetAllSchoolsAsync()
    {
        try
        {
            return await _context.Schools.OrderBy(s => s.Name).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all schools");
            return new List<School>();
        }
    }

    public async Task<School> CreateSchoolAsync(School school)
    {
        try
        {
            school.CreatedAt = DateTime.UtcNow;
            school.UpdatedAt = DateTime.UtcNow;
            _context.Schools.Add(school);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Created school: {Name}", school.Name);
            return school;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating school: {Name}", school.Name);
            throw;
        }
    }

    public async Task<School> UpdateSchoolAsync(School school)
    {
        try
        {
            school.UpdatedAt = DateTime.UtcNow;
            _context.Schools.Update(school);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Updated school: {Id}", school.Id);
            return school;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating school: {Id}", school.Id);
            throw;
        }
    }

    public async Task<bool> DeleteSchoolAsync(int id)
    {
        try
        {
            var school = await _context.Schools.FirstOrDefaultAsync(s => s.Id == id);
            if (school == null)
            {
                _logger.LogWarning("School not found for delete: {Id}", id);
                return false;
            }

            _context.Schools.Remove(school);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Deleted school: {Id}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting school: {Id}", id);
            return false;
        }
    }

    public async Task<List<School>> GetSchoolsByCityAsync(string city)
    {
        try
        {
            return await _context.Schools.Where(s => s.City == city).OrderBy(s => s.Name).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting schools by city: {City}", city);
            return new List<School>();
        }
    }
}
