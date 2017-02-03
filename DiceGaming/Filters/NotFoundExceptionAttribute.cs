using DiceGaming.Exceptions;
using System.Net;
using System.Net.Http;
using System.Web.Http.Filters;

namespace DiceGaming.Filters
{
    public class NotFoundExceptionAttribute : ExceptionFilterAttribute
    {
        public override void OnException(HttpActionExecutedContext actionExecutedContext)
        {
            if (actionExecutedContext.Exception is NotFoundException)
                actionExecutedContext.Response = actionExecutedContext.Request.CreateResponse(HttpStatusCode.NotFound,
                    actionExecutedContext.Exception.Message);

            base.OnException(actionExecutedContext);
        }
    }
}