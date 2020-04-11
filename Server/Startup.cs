using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Soundbox
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSignalR().AddNewtonsoftJsonProtocol();

            services.AddSingleton<Soundbox>();
            services.AddTransient<ISoundChainPlaybackService,DefaultSoundChainPlaybackService>();

            //setup the playback and volume services
            Type playBackType = null;
            Type volumeType = null;
            Type metaDataType = null;
            
            if(System.Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                playBackType = typeof(Playback.IrrKlang.IrrKlangSoundPlaybackService);
            }

            if(playBackType == null)
            {
                playBackType = typeof(SimpleDummySoundPlaybackService);
            }
            services.AddTransient(typeof(ISoundPlaybackService), playBackType);

            //player backend if required
            if(playBackType == typeof(Playback.IrrKlang.IrrKlangSoundPlaybackService))
            {
                //add irrKlang sound engine
                services.AddSingleton<Playback.IrrKlang.DefaultIrrKlangEngineProvider>();
                services.AddSingleton<Playback.IrrKlang.IIrrKlangEngineProvider>(provider => provider.GetService<Playback.IrrKlang.DefaultIrrKlangEngineProvider>());
                services.AddSingleton<IVolumeService>(provider => provider.GetService<Playback.IrrKlang.DefaultIrrKlangEngineProvider>());

                volumeType = typeof(Playback.IrrKlang.DefaultIrrKlangEngineProvider);

                metaDataType = typeof(Playback.IrrKlang.IrrKlangMetaDataProvider);
                services.AddTransient(typeof(IMetaDataProvider), metaDataType);
            }

            //volume
            if(volumeType == null)
            {
                volumeType = typeof(DummyVolumeService);
                services.AddSingleton(typeof(IVolumeService), volumeType);
            }

            //add virtual volume service if applicable
            if (typeof(ISoundPlaybackVirtualVolumeService).IsAssignableFrom(playBackType) && typeof(IVirtualVolumeServiceCoop).IsAssignableFrom(volumeType))
            {
                services.AddSingleton<IVirtualVolumeService, DefaultVirtualVolumeService>();
            }

            //config, database and preferences
            services.AddSingleton<ISoundboxConfigProvider,DefaultSoundboxConfigProvider>();
            services.AddSingleton<IDatabaseProvider,LiteDbDatabaseProvider>();

            services.AddSingleton<LiteDbPreferencesDatabaseProvider>();
            services.AddTransient<IPreferencesProvider<int>, LiteDbPreferencesProvider<int>>();
            services.AddTransient<IPreferencesProvider<double>, LiteDbPreferencesProvider<double>>();

            services.AddControllers().AddNewtonsoftJson();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseDefaultFiles(new DefaultFilesOptions
            {
                DefaultFileNames = new List<string> { "index.html" }
            });
            app.UseStaticFiles();

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<SoundboxHub>("/api/v1/ws");
                endpoints.MapControllers();
            });
        }
    }
}
