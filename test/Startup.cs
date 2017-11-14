using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SocketCore.Server.AspNetCore;
using Microsoft.AspNetCore.Http;
using SocketCore.Server.AspNetCore.Workflows;

namespace SocketCore.Server.AspNetCore.Tests
{
    internal class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IConnectionManager, RedisConnectionManager>(provider => new RedisConnectionManager("localhost"));
            services.AddSingleton<IWorkflowManager, RedisWorkflowManager>(provider => new RedisWorkflowManager("localhost"));
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            app.UseSocketCore("/simple", new SimpleConnection());
            app.UseSocketCore("/longrunning", new LongRunningConnection());
            app.UseSocketCoreWorkflows("/workflows");
        }

        public void ConfigureEnvironment(IHostingEnvironment env)
        {
        }
    }
}