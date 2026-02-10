namespace api_gateway.Data.Models;

public class License
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    
    public string LicenseKey { get; set; } = string.Empty;
    public string LicenseType { get; set; } = string.Empty; // Basic, Professional, Enterprise
    public string Status { get; set; } = "Active"; // Active, Suspended, Expired, Revoked
    
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RenewedAt { get; set; }
    
    public int MaxDevices { get; set; } = 1;
    public int MaxUsers { get; set; } = 1;
    public int CurrentUserCount { get; set; } = 0;

    public string? Notes { get; set; }
    public bool IsActive => Status == "Active" && ExpiresAt > DateTime.UtcNow;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class LicenseAuditLog
{
    public int Id { get; set; }
    public int LicenseId { get; set; }
    public License License { get; set; } = null!;
    
    public string Action { get; set; } = string.Empty; // Created, Updated, Renewed, Suspended, Revoked
    public string? Details { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
