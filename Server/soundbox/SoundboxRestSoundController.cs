using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Soundbox
{
    [Route("api/v1/rest/sound")]
    [ApiController]
    public class SoundboxRestSoundController : ControllerBase
    {
        /// <summary>
        /// Uploads a new <see cref="Sound"/> via <see cref="Soundbox.UploadSound(Stream, Sound, SoundboxDirectory)"/>
        /// </summary>
        /// <param name="sound">
        /// Information used when adding the new sound:<list type="bullet">
        /// <item><see cref="SoundboxFile.Name"/></item>
        /// <item><see cref="SoundboxFile.FileName"/></item>
        /// <item><see cref="SoundboxFile.Tags"/></item>
        /// </list>
        /// </param>
        /// <param name="directory"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<FileResult> Post([FromQuery] string sound, [FromQuery] string directory = null)
        {
            Sound soundActual = null;
            if(!string.IsNullOrWhiteSpace(sound))
            {
                soundActual = JsonConvert.DeserializeObject<Sound>(sound);
                soundActual.ID = SoundboxFile.ID_DEFAULT_NEW_ITEM;
            }

            SoundboxDirectory directoryActual = null;
            if(!string.IsNullOrWhiteSpace(directory))
            {
                directoryActual = JsonConvert.DeserializeObject<SoundboxDirectory>(directory);
            }

            var soundbox = HttpContext.RequestServices.GetService(typeof(Soundbox)) as Soundbox;
            return await soundbox.UploadSound(Request.Body, soundActual, directoryActual);
        }
    }
}
