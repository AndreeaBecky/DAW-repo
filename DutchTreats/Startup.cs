﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using DutchTreats.Data;
using DutchTreats.Data.Entities;
using DutchTreats.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace DutchTreats
{
    public class Startup
    {
        private readonly IConfiguration _config;

        public Startup(IConfiguration config)
        {
            _config = config;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            // store the info about a user
            services.AddIdentity<StoreUser, IdentityRole>(cfg =>
            {
                cfg.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<DutchContext>();

            services.AddAuthentication()
                    .AddCookie()
                    .AddJwtBearer(cfg =>
                    {
                        cfg.TokenValidationParameters = new TokenValidationParameters()
                        {
                            ValidIssuer = _config["Tokens:Issuer"],
                            ValidAudience = _config["Tokens:Audience"],
                            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Tokens:Key"]))
                        };
                    });


            // creating a DbContext (scoped) to create and use entities within EF
            services.AddDbContext<DutchContext>(cfg =>
            {
                cfg.UseSqlServer(_config.GetConnectionString("DutchConnectionString"));
            });

#pragma warning disable CS0618 // Type or member is obsolete
            services.AddAutoMapper();

            // it's gonna be creatable through the denepdency injection / adding the service to seed the Db
            services.AddTransient<DutchSeeder>();

            // add IDutchRepository as a service, but use the implementation for DutchRepository (<- the mock)
            services.AddScoped<IDutchRepository, DutchRepository>();

            services.AddTransient<IMailService, NullMailService>();
            // add suport for real mail service

            services.AddMvc()
                 // handling reference loop
                 .AddJsonOptions(opt => opt.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore)
                 // sets the compatibility for filters in ProdCtrll which validates the response type, what the controllers produces or trowing standard exceptions (eg: when the modelState is invalid)
                 .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            //    it looks in the default root directory(wwwroot) and changes the path to localhost:port(here: 8888)
            //    app.UseDefaultFiles(); // we commented it because we need the MVC, not the html default file

            if (env.IsDevelopment())
            {
                //    it shows a 'developer exception page' for exceptions
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/error");
            }

            app.UseStaticFiles();

            app.UseAuthentication();

            app.UseMvc(cfg =>
            {
                cfg.MapRoute("Default",
                "{controller}/{action}/{id?}",
                new
                {
                    controller = "App",
                    Action = "Index"
                });
            });

            // middleware to serve files from the node_modules directory in the root of the project (ODETOCODE)
            app.UseNodeModules(env);

        }

    }

}
