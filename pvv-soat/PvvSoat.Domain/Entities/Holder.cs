namespace PvvSoat.Domain.Entities;

public class Holder
{
    public Guid HolderId { get; set; }
    public string DNI { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Budget> Budgets { get; set; } = new List<Budget>();
    public ICollection<Policy> Policies { get; set; } = new List<Policy>();
}
