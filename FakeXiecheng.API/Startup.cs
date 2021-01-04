using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AutoMapper;
using FakeXiecheng.API.Database;
using FakeXiecheng.API.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Serialization;

namespace FakeXiecheng.API
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        public void ConfigureServices(IServiceCollection services)
        {
           
            services.AddControllers(setupAction =>
            {
                setupAction.ReturnHttpNotAcceptable = true;
                //setupAction.OutputFormatters.Add(
                //    new XmlDataContractSerializerOutputFormatter());
            }).AddNewtonsoftJson(setupAction =>
                {
                    setupAction.SerializerSettings.ContractResolver = 
                        new CamelCasePropertyNamesContractResolver();
                })
                .AddXmlDataContractSerializerFormatters()
                .ConfigureApiBehaviorOptions(setupAction =>
                {
                    setupAction.InvalidModelStateResponseFactory = context =>
                    {
                        var problemDetail = new ValidationProblemDetails(context.ModelState)
                        {
                            Type = "not important",
                            Title = "Data validation fail",
                            Status = StatusCodes.Status422UnprocessableEntity,
                            Detail = "Please check detail",
                            Instance = context.HttpContext.Request.Path
                        };
                        problemDetail.Extensions.Add("traceId", context.HttpContext.TraceIdentifier);
                        return new UnprocessableEntityObjectResult(problemDetail)
                        {
                            ContentTypes = {"application/probnlem+json"}
                        };
                    };
                });
            services.AddTransient<ITouristRouteRepository, TouristRouteRepository>();
            //services.AddScoped();
            //services.AddSingleton();
            services.AddDbContext<AppDbContext>(option =>
            {
                option.UseSqlServer(Configuration["DbContext:ConnectionString"]);
                //option.UseMySql(Configuration["DbContext:MySQLConnectionString"]);
            });

            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}