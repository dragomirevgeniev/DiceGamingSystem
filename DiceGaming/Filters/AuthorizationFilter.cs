using DiceGaming.Data;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace DiceGaming.Filters
{
    public class AuthorizationFilter : AuthorizationFilterAttribute
    {
        public override void OnAuthorization(HttpActionContext actionContext)
        {
            if (actionContext.ActionDescriptor.GetCustomAttributes<AllowAnonymousAttribute>().Any()
                       || actionContext.ControllerContext.ControllerDescriptor.GetCustomAttributes<AllowAnonymousAttribute>().Any())
                return;

            IEnumerable<string> values;
            if (actionContext.Request.Headers.TryGetValues("AuthToken", out values))
            {
                var token = values.FirstOrDefault();
                if (token != null)
                {
                    using (var db = new DiceGamingDb())
                    {
                        if (db.Logins.FirstOrDefault(login => object.Equals(login.Token, token)) != null)
                        {
                            return;
                        }
                    }
                }
            }

            actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Unauthorized, "No rights to access this resource!");
        }
    }
}