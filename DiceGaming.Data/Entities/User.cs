using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace DiceGaming.Data.Entities
{
    public class User
    {
        [Key(), DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string UserName { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string Salt { get; set; }

        
        public string FullName { get; set; }

        
        public string Email { get; set; }

        public decimal VirtualMoney { get; set; }

        public ICollection<Game> Games { get; set; }

        public ICollection<Login> Logins { get; set; }
    }
}
