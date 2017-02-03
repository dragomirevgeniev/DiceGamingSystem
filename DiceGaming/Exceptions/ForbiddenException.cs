using System.Web;

namespace DiceGaming.Exceptions
{
    public class ForbiddenException : HttpException
    {
        public ForbiddenException() : base("Access denied!") { }
        public ForbiddenException(string message) : base(message) { }
    }
}