using System.Security.Cryptography;
using System.Text;
using api_gateway.Data;
using api_gateway.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace api_gateway.Services;

public interface ILicenseService
{
    Task<License?> GetLicenseByUserIdAsync(int userId);
    Task<License?> GetLicenseByKeyAsync(string licenseKey);
    Task<License> CreateLicenseAsync(int userId, string licenseType, int expiryDays, int maxUsers = 1);
    Task<License> UpdateLicenseAsync(License license);
    Task<bool> RenewLicenseAsync(int licenseId, int expiryDays);
    Task<bool> SuspendLicenseAsync(int licenseId);
    Task<bool> RevokeLicenseAsync(int licenseId);
    Task<bool> IsLicenseValidAsync(string licenseKey);
    Task<List<License>> GetLicensesByStatusAsync(string status);
    Task<List<License>> GetExpiredLicensesAsync();
    Task<bool> AddUserAsync(int licenseId);
    Task<bool> RemoveUserAsync(int licenseId);
    Task<List<LicenseAuditLog>> GetLicenseAuditLogsAsync(int licenseId);
}

public class LicenseService : ILicenseService
{
    private readonly ApiGatewayDbContext _context;
    private readonly ILogger<LicenseService> _logger;

    public LicenseService(ApiGatewayDbContext context, ILogger<LicenseService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<License?> GetLicenseByUserIdAsync(int userId)
    {
        try
        {
            return await _context.Licenses
                .Include(l => l.User)
                .FirstOrDefaultAsync(l => l.UserId == userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting license for user: {UserId}", userId);
            return null;
        }
    }

    public async Task<License?> GetLicenseByKeyAsync(string licenseKey)
    {
        try
        {
            return await _context.Licenses
                .Include(l => l.User)
                .FirstOrDefaultAsync(l => l.LicenseKey == licenseKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting license by key");
            return null;
        }
    }

    public async Task<License> CreateLicenseAsync(int userId, string licenseType, int expiryDays, int maxUsers = 1)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                throw new InvalidOperationException($"User with ID {userId} not found");
            }

            var license = new License
            {
                UserId = userId,
                LicenseKey = GenerateLicenseKey(),
                LicenseType = licenseType,
                Status = "Active",
                IssuedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(expiryDays),
                MaxUsers = maxUsers,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Licenses.Add(license);
            await _context.SaveChangesAsync();

            await LogAuditAsync(license.Id, "Created", $"License created for {licenseType}", "System");

            _logger.LogInformation("License created for user: {UserId}, Key: {LicenseKey}", userId, license.LicenseKey);
            return license;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating license for user: {UserId}", userId);
            throw;
        }
    }

    public async Task<License> UpdateLicenseAsync(License license)
    {
        try
        {
            license.UpdatedAt = DateTime.UtcNow;
            _context.Licenses.Update(license);
            await _context.SaveChangesAsync();

            _logger.LogInformation("License updated: {LicenseId}", license.Id);
            return license;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating license: {LicenseId}", license.Id);
            throw;
        }
    }

    public async Task<bool> RenewLicenseAsync(int licenseId, int expiryDays)
    {
        try
        {
            var license = await _context.Licenses.FirstOrDefaultAsync(l => l.Id == licenseId);
            if (license == null)
            {
                _logger.LogWarning("License not found for renewal: {LicenseId}", licenseId);
                return false;
            }

            license.ExpiresAt = DateTime.UtcNow.AddDays(expiryDays);
            license.RenewedAt = DateTime.UtcNow;
            license.UpdatedAt = DateTime.UtcNow;

            _context.Licenses.Update(license);
            await _context.SaveChangesAsync();

            await LogAuditAsync(licenseId, "Renewed", $"License renewed for {expiryDays} days", "System");

            _logger.LogInformation("License renewed: {LicenseId}, New expiry: {ExpiryDate}", licenseId, license.ExpiresAt);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error renewing license: {LicenseId}", licenseId);
            return false;
        }
    }

    public async Task<bool> SuspendLicenseAsync(int licenseId)
    {
        try
        {
            var license = await _context.Licenses.FirstOrDefaultAsync(l => l.Id == licenseId);
            if (license == null)
            {
                _logger.LogWarning("License not found for suspension: {LicenseId}", licenseId);
                return false;
            }

            license.Status = "Suspended";
            license.UpdatedAt = DateTime.UtcNow;

            _context.Licenses.Update(license);
            await _context.SaveChangesAsync();

            await LogAuditAsync(licenseId, "Suspended", "License suspended", "System");

            _logger.LogInformation("License suspended: {LicenseId}", licenseId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suspending license: {LicenseId}", licenseId);
            return false;
        }
    }

    public async Task<bool> RevokeLicenseAsync(int licenseId)
    {
        try
        {
            var license = await _context.Licenses.FirstOrDefaultAsync(l => l.Id == licenseId);
            if (license == null)
            {
                _logger.LogWarning("License not found for revocation: {LicenseId}", licenseId);
                return false;
            }

            license.Status = "Revoked";
            license.UpdatedAt = DateTime.UtcNow;

            _context.Licenses.Update(license);
            await _context.SaveChangesAsync();

            await LogAuditAsync(licenseId, "Revoked", "License revoked", "System");

            _logger.LogInformation("License revoked: {LicenseId}", licenseId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking license: {LicenseId}", licenseId);
            return false;
        }
    }

    public async Task<bool> IsLicenseValidAsync(string licenseKey)
    {
        try
        {
            var license = await _context.Licenses.FirstOrDefaultAsync(l => l.LicenseKey == licenseKey);
            if (license == null)
            {
                return false;
            }

            return license.IsActive && license.CurrentUserCount < license.MaxUsers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating license");
            return false;
        }
    }

    public async Task<List<License>> GetLicensesByStatusAsync(string status)
    {
        try
        {
            return await _context.Licenses
                .Include(l => l.User)
                .Where(l => l.Status == status)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting licenses by status: {Status}", status);
            return new List<License>();
        }
    }

    public async Task<List<License>> GetExpiredLicensesAsync()
    {
        try
        {
            return await _context.Licenses
                .Include(l => l.User)
                .Where(l => l.ExpiresAt < DateTime.UtcNow && l.Status == "Active")
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expired licenses");
            return new List<License>();
        }
    }

    public async Task<bool> AddUserAsync(int licenseId)
    {
        try
        {
            var license = await _context.Licenses.FirstOrDefaultAsync(l => l.Id == licenseId);
            if (license == null)
            {
                _logger.LogWarning("License not found: {LicenseId}", licenseId);
                return false;
            }

            if (license.CurrentUserCount >= license.MaxUsers)
            {
                _logger.LogWarning("Max user assignment limit reached for license: {LicenseId}", licenseId);
                return false;
            }

            license.CurrentUserCount++;
            license.UpdatedAt = DateTime.UtcNow;

            _context.Licenses.Update(license);
            await _context.SaveChangesAsync();
            await LogAuditAsync(licenseId, "User Assigned", $"User assigned. Total assigned: {license.CurrentUserCount}", "System");

            _logger.LogInformation("User assigned to license: {LicenseId}, Total assigned: {UserCount}", licenseId, license.CurrentUserCount);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding user to license: {LicenseId}", licenseId);
            return false;
        }
    }

    public async Task<bool> RemoveUserAsync(int licenseId)
    {
        try
        {
            var license = await _context.Licenses.FirstOrDefaultAsync(l => l.Id == licenseId);
            if (license == null)
            {
                _logger.LogWarning("License not found: {LicenseId}", licenseId);
                return false;
            }

            if (license.CurrentUserCount > 0)
            {
                license.CurrentUserCount--;
                license.UpdatedAt = DateTime.UtcNow;

                _context.Licenses.Update(license);
                await _context.SaveChangesAsync();

                await LogAuditAsync(licenseId, "User Removed", $"User removed. Total assigned: {license.CurrentUserCount}", "System");

                _logger.LogInformation("User removed from license: {LicenseId}, Total assigned: {UserCount}", licenseId, license.CurrentUserCount);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing user from license: {LicenseId}", licenseId);
            return false;
        }
    }

    public async Task<List<LicenseAuditLog>> GetLicenseAuditLogsAsync(int licenseId)
    {
        try
        {
            return await _context.LicenseAuditLogs
                .Where(l => l.LicenseId == licenseId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit logs for license: {LicenseId}", licenseId);
            return new List<LicenseAuditLog>();
        }
    }

    private string GenerateLicenseKey()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        var builder = new StringBuilder();

        for (int i = 0; i < 32; i++)
        {
            if (i > 0 && i % 8 == 0)
                builder.Append('-');

            builder.Append(chars[random.Next(chars.Length)]);
        }

        return builder.ToString();
    }

    private async Task LogAuditAsync(int licenseId, string action, string? details, string changedBy)
    {
        try
        {
            var auditLog = new LicenseAuditLog
            {
                LicenseId = licenseId,
                Action = action,
                Details = details,
                ChangedBy = changedBy,
                CreatedAt = DateTime.UtcNow
            };

            _context.LicenseAuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging audit for license: {LicenseId}", licenseId);
        }
    }
}
