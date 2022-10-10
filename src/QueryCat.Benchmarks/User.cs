using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using QueryCat.Backend.Relational;

namespace QueryCat.Benchmarks;

public class User
{
    [Column(name: "id")]
    [Key]
    public int Id { get; private set; }

    [Column(name: "email")]
    [Required]
    [EmailAddress]
    [MaxLength(100)]
    public string Email { get; set; }

    [Column(name: "first_name")]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Column(name: "last_name")]
    [MaxLength(100)]
    [Required]
    public string LastName { get; set; } = string.Empty;

    [Column(name: "email_verified_at")]
    public DateTime? EmailVerifiedAt { get; set; }

    [Column(name: "address")]
    [MaxLength(100)]
    [Required]
    public string Address { get; set; } = string.Empty;

    [Column(name: "state")]
    [MaxLength(2)]
    [Required]
    public string State { get; set; } = string.Empty;

    [Column(name: "zip")]
    [MaxLength(12)]
    [Required]
    public string Zip { get; set; } = string.Empty;

    [Column(name: "phone")]
    [MaxLength(50)]
    [Required]
    public string Phone { get; set; } = string.Empty;

    [Column(name: "gender")]
    [Required]
    public Gender Gender { get; set; } = Gender.Unknown;

    [Column(name: "dob")]
    [Required]
    public DateTime? DateOfBirth { get; set; }

    [Column(name: "balance")]
    [Required]
    public decimal Balance { get; set; }

    [Column(name: "phrase")]
    [Required]
    public string Phrase { get; set; } = string.Empty;

    [Column(name: "created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column(name: "removed_at")]
    public DateTime? RemovedAt { get; set; }

    public static ClassBuilder<User> ClassBuilder { get; } = new ClassBuilder<User>()
        .AddProperty(u => u.Id)
        .AddProperty(u => u.Email)
        .AddProperty(u => u.FirstName)
        .AddProperty(u => u.LastName)
        .AddProperty(u => u.EmailVerifiedAt)
        .AddProperty(u => u.Address)
        .AddProperty(u => u.State)
        .AddProperty(u => u.Zip)
        .AddProperty(u => u.Phone)
        .AddProperty(u => u.Gender)
        .AddProperty(u => u.DateOfBirth)
        .AddProperty(u => u.Balance)
        .AddProperty(u => u.CreatedAt)
        .AddProperty(u => u.RemovedAt)
        .AddProperty(u => u.Phrase);

    /// <summary>
    /// Constructor for faker.
    /// </summary>
    public User() : this(-1, string.Empty)
    {
    }

    public User(int id, string email)
    {
        Id = id;
        Email = email;
    }
}
