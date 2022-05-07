using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Soundbox
{
    public class Startup
    {
        protected IConfiguration Configuration;

        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddEventLog(settings =>
                {
                    //settings.SourceName = "Soundbox";
                });
                logging.AddConsole();
            });

            services.AddCors(); 
            services.AddSignalR().AddNewtonsoftJsonProtocol();


            bool isWindows = System.Environment.OSVersion.Platform == PlatformID.Win32NT;

            services.Configure<AppSettings.SoundboxAppSettings>(Configuration.GetSection("Soundbox"));

            services.AddSingleton<Soundbox>();
            services.AddTransient<ISoundChainPlaybackService,DefaultSoundChainPlaybackService>();

            //setup the playback and volume services
            Type playBackType = null;
            Type volumeType = null;
            Type metaDataType = null;
            
            if(isWindows)
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

            //audio processing
            //NAudio
            services.AddTransient<Audio.IDeviceStreamAudioSourceProvider, Audio.NAudio.NAudioStreamAudioSourceProvider>();
            services.AddTransient<Audio.Processing.IStreamAudioResamplerProvider, Audio.NAudio.NAudioStreamAudioResamplerProvider>();
            if (isWindows)
            {
                services.AddTransient<Audio.NAudio.NAudioWasapiStreamAudioSource>();
                services.AddTransient<Audio.NAudio.NAudioMediaFoundationStreamAudioResampler>();
            }

            //effects
            services.AddTransient<Audio.Processing.Noisegate.INoiseGateStreamAudioProcessProvider, Audio.Processing.Noisegate.Implementation.DefaultNoiseGateStreamAudioProcessProvider>();
            services.AddTransient<Audio.Processing.Noisegate.Implementation.DefaultNoiseGateStreamAudioProcessor>();

            //speech recognition
            services.Configure<Speech.Recognition.AppSettings.SpeechRecognitionAppSettings>(Configuration.GetSection("Soundbox.SpeechRecognition"));
            services.AddTransient<Speech.Recognition.ISpeechRecognitionServiceProvider, Speech.Recognition.Azure.AzureSpeechRecognitionServiceProvider>();
            services.AddTransient<Speech.Recognition.Azure.AzureSpeechRecognitionService>();
            services.AddTransient<Speech.Recognition.SpeechRecognizedEvent>();

            services.AddControllers().AddNewtonsoftJson();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            Logging.StaticLoggerProvider.LoggerFactory = loggerFactory;

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            //TODO for angular test (with ng serve)
            app.UseCors(
                options => options
                    .AllowAnyHeader()
                    .AllowCredentials()
                    .WithMethods("POST")
                    .SetIsOriginAllowed(origin => true)
            );

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
