using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Soundbox.Server
{
    [Route("api/v1/rest/sound")]
    [ApiController]
    public class SoundboxRestSoundController : ControllerBase
    {
        // POST api/<controller>
        [HttpPost]
        public async Task Post([FromQuery] string name)
        {
            Sound sound = new Sound()
            {
                ID = SoundboxFile.ID_DEFAULT_NEW_ITEM,
                FileName = name,
                Name = name
            };

            var soundbox = HttpContext.RequestServices.GetService(typeof(Soundbox)) as Soundbox;
            await soundbox.UploadSound(Request.Body, sound, null);
        }

        public int Get()
        {
            return 5;
        }
    }
}
