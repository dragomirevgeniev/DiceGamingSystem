using System.Web;

namespace DiceGaming.Exceptions
{
    public class NotFoundException : HttpException
    {
        public NotFoundException() : base("Not found!") { }
        public NotFoundException(string message) : base(message) { }
    }
}