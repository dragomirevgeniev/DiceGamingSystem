using System.Web;

namespace DiceGaming.Exceptions
{
    public class UnauthorizedException : HttpException
    {
        public UnauthorizedException() : base("Unauthorized!") { }
        public UnauthorizedException(string message) : base(message) { }
    }
}