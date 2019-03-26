using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Mongo;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace scheduller.api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            services.AddHangfire(config =>
            {
                config.UseMongoStorage("mongodb://localhost", "local");
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHangfireDashboard("/dashboard");
            app.UseHangfireServer();
            app.UseHttpsRedirection();
            app.UseMvc();
            
            RecurringJob.AddOrUpdate(
                () => Console.WriteLine("Minutely Job"), Cron.Minutely);
            
            List<Assembly> allAssemblies = new List<Assembly>();
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            foreach (string dll in Directory.GetFiles(path, "*.dll"))
            {
                var assembly = Assembly.LoadFile((dll));

                foreach (Type specificDll in Assembly.GetExecutingAssembly().GetTypes()
                    .Where(specificDll => specificDll.GetInterfaces().Contains(typeof(ITrigger))))
                {
                    object obj = Activator.CreateInstance(specificDll);
                    MethodInfo methodInfo = specificDll.GetMethod("startRunner");
                    methodInfo.Invoke(obj, null);
                }  
            }

            
            //if (String.IsNullOrEmpty(path)) { return null; } //sanity check

            DirectoryInfo info = new DirectoryInfo(path);
            //if (!info.Exists) { return null; } //make sure directory exists

            var implementors = new List<ITrigger>();

            foreach (FileInfo file in info.GetFiles("*.dll")) //loop through all dll files in directory
            {
                Assembly currentAssembly = null;
                try
                {
                    var name = AssemblyName.GetAssemblyName(file.FullName);
                    currentAssembly = Assembly.Load(name);
                }
                catch (Exception ex)
                {
                    continue;
                }

                currentAssembly.GetTypes()
                    .Where(t => t != typeof(ITrigger) && typeof(ITrigger).IsAssignableFrom(t))
                    .ToList()
                    .ForEach(x => implementors.Add((ITrigger)Activator.CreateInstance(x)));

                MethodInfo met =  currentAssembly.GetType().GetMethod("startRunner");
                met.Invoke(currentAssembly, null);
            }

            foreach (var dll in implementors)
            {   
                
            }
            
            
        }
    }
}