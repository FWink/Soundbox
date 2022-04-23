﻿using Microsoft.Extensions.Logging;
using System;

namespace Soundbox.Audio.NAudio
{
    /// <summary>
    /// Factory for <see cref="IStreamAudioSource"/>s using the NAudio library.
    /// </summary>
    public class NAudioStreamAudioSourceProvider : IStreamAudioSourceProvider
    {
        protected IServiceProvider ServiceProvider;
        protected ILogger Logger;

        public NAudioStreamAudioSourceProvider(IServiceProvider serviceProvider, ILogger<NAudioStreamAudioSourceProvider> logger)
        {
            this.ServiceProvider = serviceProvider;
            this.Logger = logger;
        }

        public IStreamAudioSource GetStreamAudioSource(AudioDevice device)
        {
            var naudioDevice = NAudioUtilities.GetDevice(device);
            if (naudioDevice == null)
            {
                //no device found
                return null;
            }

            //try WASAPI first
            var wasapiSource = ServiceProvider.GetService(typeof(NAudioWasapiStreamAudioSource)) as NAudioWasapiStreamAudioSource;
            if (wasapiSource != null)
            {
                //WASAPI is available
                try
                {
                    wasapiSource.SetAudioDevice(device);
                    return wasapiSource;
                }
                catch (Exception e)
                {
                    //not supported?
                    Logger.LogError(e, "Could not initialize NAudio WASAPI source");
                    wasapiSource.Dispose();
                }
            }

            return null;
        }
    }
}
