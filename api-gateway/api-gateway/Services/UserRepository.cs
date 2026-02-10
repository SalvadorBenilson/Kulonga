using api_gateway.Data;
using api_gateway.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace api_gateway.Services;

public interface IUserRepository
{
    Task<User?> GetUserByUsernameAsync(string username);
    Task<User?> GetUserByEmailAsync(string email);
    Task<User?> GetUserByIdAsync(int id);
    Task<User> CreateUserAsync(User user);
    Task<User> UpdateUserAsync(User user);
    Task<bool> DeleteUserAsync(int id);
    Task<List<string>> GetUserRolesAsync(int userId);
    Task<bool> AddUserRoleAsync(int userId, int roleId);
}

public class UserRepository : IUserRepository
{
    private readonly ApiGatewayDbContext _context;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(ApiGatewayDbContext context, ILogger<UserRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        try
        {
            return await _context.Users.FirstOrDefaultAsync(u => 
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by username: {Username}", username);
            return null;
        }
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        try
        {
            return await _context.Users.FirstOrDefaultAsync(u => 
                u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by email: {Email}", email);
            return null;
        }
    }

    public async Task<User?> GetUserByIdAsync(int id)
    {
        try
        {
            return await _context.Users.Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by id: {UserId}", id);
            return null;
        }
    }

    public async Task<User> CreateUserAsync(User user)
    {
        try
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            _logger.LogInformation("User created: {Username}", user.Username);
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user: {Username}", user.Username);
            throw;
        }
    }

    public async Task<User> UpdateUserAsync(User user)
    {
        try
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            _logger.LogInformation("User updated: {Username}", user.Username);
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user: {Username}", user.Username);
            throw;
        }
    }

    public async Task<bool> DeleteUserAsync(int id)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return false;
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            _logger.LogInformation("User deleted: {UserId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user: {UserId}", id);
            throw;
        }
    }

    public async Task<List<string>> GetUserRolesAsync(int userId)
    {
        try
        {
            return await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.Role.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user roles: {UserId}", userId);
            return new List<string>();
        }
    }

    public async Task<bool> AddUserRoleAsync(int userId, int roleId)
    {
        try
        {
            var userRole = new UserRole { UserId = userId, RoleId = roleId };
            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Role added to user: UserId={UserId}, RoleId={RoleId}", userId, roleId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding role to user: UserId={UserId}, RoleId={RoleId}", userId, roleId);
            return false;
        }
    }
}
