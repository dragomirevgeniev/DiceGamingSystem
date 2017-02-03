using System.Web;

namespace DiceGaming.Exceptions
{
    public class ConflictException : HttpException
    {
        public ConflictException() : base("Conflict!") { }
        public ConflictException(string message) : base(message) { }
    }
}