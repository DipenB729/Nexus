using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AngularApp4.Model
{
    public class Service
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public string Category { get; set; } = string.Empty;

        [Required]
        public int DurationMinutes { get; set; } // We store as integer for easy math

        [Required]
        [Column(TypeName = "decimal(18,2)")] // Accurate for currency
        public decimal Price { get; set; }

        public string Icon { get; set; } = "settings";

        public string Color { get; set; } = "#3b82f6"; // Default blue hex

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
