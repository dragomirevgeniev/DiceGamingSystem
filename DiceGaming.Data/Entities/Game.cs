using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;

namespace DiceGaming.Data.Entities
{
    public class Game
    {
        [Key(), DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [Required]
        public int DiceSumBet { get; set; }

        [Required]
        public int DiceSumResult { get; set; }

        [Required]
        public decimal Stake { get; set; }

        [Required]
        public decimal Win { get; set; }

        [Required]
        public DateTime CreationDate { get; set; }
    }
}
