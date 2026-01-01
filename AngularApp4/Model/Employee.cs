using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AngularApp4.Model
{
 
 
        public class Employee
        {
            [Key] 
            public int Id { get; set; }

            [Required] 
            public string FirstName { get; set; } = string.Empty;

            [Required] 
            public string LastName { get; set; } = string.Empty;

            [Required]
            [EmailAddress]  
            public string Email { get; set; } = string.Empty;
 
            [Column(TypeName = "decimal(18,2)")]
            public decimal Salary { get; set; }

            public string Department { get; set; } = string.Empty;

            public DateTime DateHired { get; set; } = DateTime.Now;
        }
 
}
