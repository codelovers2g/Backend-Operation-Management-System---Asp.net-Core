   //To run Hangfire functionality add functionality in statup file
   public void ConfigureServices(IServiceCollection services)
        {
            // Add Hangfire services.
            services.AddHangfire(configuration => configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(Configuration.GetConnectionString("StarterCode")));

            // Add the processing server as IHostedService
            services.AddHangfireServer();

        }


        [Obsolete]
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider svp)
        {
            app.UseHangfireServer();
            app.UseHangfireDashboard();

            //call service to run by hangfire
            RecurringJob.AddOrUpdate<IProductDetailsService>(x => x.SaveProductDetails(), Cron.MinuteInterval(4));
            RecurringJob.AddOrUpdate<IRequestDetailsService>(x => x.CreateRequestFromMail(), Cron.MinuteInterval(6));
        }
