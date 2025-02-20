﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Base;
using Exceptionless;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FarTravelNet.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        /// <summary>
        /// Swagger项目名字
        /// </summary>
        public const string _Project_Name = "FarTravelNet.Api";


        #region 将服务添加到容器，也就是注入

        // 此方法由运行时调用。 使用此方法将服务添加到容器。
        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            #region 模块化注入

            //模块化注入需要在控制器写一个构造函数来接收注入的值，然后在把值赋值给控制器里面的参数
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            //注入JWT的加密验证字段
            services.AddSingleton(new ApiTokenConfig("A3FFB16D-D2C0-4F25-BACE-1B9E5AB614A6"));
            services.AddScoped<IApiTokenService, ApiTokenService>();

            services.AddSwaggerGen(c =>
            {
                typeof(ApiVersions).GetEnumNames().ToList().ForEach(version =>
                {
                    c.SwaggerDoc(version, new Swashbuckle.AspNetCore.Swagger.Info
                    {
                        Version = version,
                        Title = $"{_Project_Name} 接口文档",
                        Description = $"{_Project_Name} HTTP API " + version,
                        TermsOfService = "None"
                    });
                });
                var basePath = Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application.ApplicationBasePath;
                var xmlPath = System.IO.Path.Combine(basePath, $"{_Project_Name}.xml");
                c.IncludeXmlComments(xmlPath);
                c.OperationFilter<AssignOperationVendorExtensions>();
                c.DocumentFilter<ApplyTagDescriptions>();
            });

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            //使用Autofac替换自带IOC
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterModule<BusinessModule>();
            containerBuilder.Populate(services);
            var container = containerBuilder.Build();
            //将container静态化到AutofacHelper中，以便于后面操作使用
            AutofacHelper.Container = container;
            return new AutofacServiceProvider(container);

            #endregion

            #region  属性注入，暂时没有用到

            //替换控制器所有者,只有替换了控制器的所有者，才能够使用Autofac的属性注入，这个替换必须在AddMvc之前
            //services.Replace(ServiceDescriptor.Transient<IControllerActivator, ServiceBasedControllerActivator>());

            //services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            //services.AddDirectoryBrowser();
            ////使用Autofac替换自带IOC，模块化注入
            //var containerBuilder = new ContainerBuilder();
            //containerBuilder.RegisterModule<BusinessModule>();
            ////采用属性注入控制器
            //containerBuilder.RegisterType<AutoDIController>().PropertiesAutowired();
            ////containerBuilder.RegisterTypes(Controllers.Select(ti => ti.AsType()).ToArray()).PropertiesAutowired();
            //containerBuilder.Populate(services);

            //var container = containerBuilder.Build();
            //return new AutofacServiceProvider(container);

            #endregion

        }

        #endregion

        #region 配置HTTP请求管道

        // 此方法由运行时调用。 使用此方法配置HTTP请求管道。
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            //判断运行环境
            if (env.IsDevelopment())
            {
                //下面是Debug环境才执行的
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    typeof(ApiVersions).GetEnumNames().OrderByDescending(e => e).ToList().ForEach(version =>
                    {
                        c.SwaggerEndpoint($"/swagger/{version}/swagger.json", $"{_Project_Name} {version}");
                    });
                    //注入汉化文件
                    //1.1.0版本才有的
                    c.InjectOnCompleteJavaScript($"/swagger_translator.js");
                });
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            ServiceLocator.Configure(app.ApplicationServices);

            #region Exceptionless.AspNetCore 日志系统
            ExceptionlessClient.Default.Configuration.ApiKey = Configuration.GetSection("Exceptionless:ApiKey").Value;
            ExceptionlessClient.Default.Configuration.ServerUrl = Configuration.GetSection("Exceptionless:ServerUrl").Value;
            app.UseExceptionless();
            #endregion

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseMvc();
        }

        #endregion

    }
}
