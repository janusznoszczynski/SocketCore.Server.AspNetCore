using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SocketCore.Server.AspNetCore;
using SocketCore.Server.AspNetCore.Workflows;

namespace host
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IConnectionManager, InProcConnectionManager>(provider => new InProcConnectionManager());
            services.AddSingleton<IWorkflowManager, InProcWorkflowManager>(provider => new InProcWorkflowManager());
            // services.AddSingleton<IConnectionManager, RedisConnectionManager>(provider => new RedisConnectionManager());
            // services.AddSingleton<IWorkflowManager, RedisWorkflowManager>(provider => new RedisWorkflowManager());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSocketCoreWorkflows("/realtime");

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Hello World!");
            });
        }
    }
}
