using Autofac;
using Autofac.Integration.WebApi;
using DiceGaming.Filters;
using Newtonsoft.Json.Serialization;
using System.Reflection;
using System.Web.Http;

namespace DiceGaming
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            config.Formatters.JsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            config.Formatters.JsonFormatter.UseDataContractJsonSerializer = false;

            config.Filters.Add(new AuthorizationFilter());
            config.Filters.Add(new BadRequestExceptionAttribute());
            config.Filters.Add(new ConflictExceptionAttribute());
            config.Filters.Add(new ForbiddenExceptionAttribute());
            config.Filters.Add(new NotFoundExceptionAttribute());
            config.Filters.Add(new UnauthorizedExceptionAttribute());
            config.Filters.Add(new DefaultExceptionAttribute());

            var builder = new ContainerBuilder();

            builder.RegisterApiControllers(Assembly.GetExecutingAssembly());
            builder.RegisterWebApiFilterProvider(config);

            var container = builder.Build();

            config.DependencyResolver = new AutofacWebApiDependencyResolver(container);

            config.EnsureInitialized();
        }
    }
}
