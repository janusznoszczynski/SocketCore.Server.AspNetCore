using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SocketCore.Server.AspNetCore;
using Microsoft.AspNetCore.Http;

namespace SocketCore.Server.AspNetCore.Tests
{
    internal class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IConnectionManager, RedisConnectionManager>(provider => new RedisConnectionManager("localhost"));
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            app.UseSocketCore("/simple", new SimpleConnection());
            app.UseSocketCore("/longrunning", new LongRunningConnection());
        }

        public void ConfigureEnvironment(IHostingEnvironment env)
        {
        }
    }
}