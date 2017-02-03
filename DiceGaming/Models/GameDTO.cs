using System;

namespace DiceGaming.Models
{
    public class GameDTO
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int Bet { get; set; }
        public int ActualRoll { get; set; }
        public decimal Stake { get; set; }
        public decimal Win { get; set; }
        public DateTime Timestamp { get; set; }
    }
}