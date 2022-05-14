using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox
{
    /// <summary>
    /// <see cref="ResultStatus"/> for volume operations (<see cref="Soundbox.SetVolume(int)"/>, <see cref="Soundbox.SetVolumeSettingMax(int)"/>).
    /// </summary>
    public class VolumeResultStatus : ResultStatus
    {
        protected const ResultStatusSegment Segment = ResultStatusSegment.Volume;

        public static readonly VolumeResultStatus INVALID_VOLUME_MAX = new VolumeResultStatus(2400, $"Invalid volume. Max volume is {Constants.VOLUME_MAX}");
        public static readonly VolumeResultStatus INVALID_VOLUME_MIN = new VolumeResultStatus(2401, $"Invalid volume. Min volume is {Constants.VOLUME_MIN}");

        private VolumeResultStatus(int code, string message, bool success = false)
        {
            Code = code;
            Message = message;
            Success = success;
        }
    }
}
