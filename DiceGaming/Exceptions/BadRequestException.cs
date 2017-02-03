using System.Web;

namespace DiceGaming.Exceptions
{
    public class BadRequestException : HttpException
    {
        public BadRequestException() : base("Bad request!") { }
        public BadRequestException(string message) : base(message) { }
    }
}