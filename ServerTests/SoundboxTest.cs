using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Soundbox.AppSettings;
using System;
using System.Collections.Generic;
using System.Text;

namespace Soundbox.Test
{
    public class SoundboxTestWrapper : Soundbox
    {
        public SoundboxTestWrapper(IServiceProvider serviceProvider, IHubContext<SoundboxHub, ISoundboxClient> hubContext, ISoundboxConfigProvider config, IDatabaseProvider database, ILogger<Soundbox> logger, IOptions<SoundboxAppSettings> appSettings) :
            base(serviceProvider, hubContext, config, database, null, logger, appSettings)
        {
        }

        [TestClass]
        public class SoundboxTest
        {
            protected IServiceProvider ServiceProvider;
            protected SoundboxTestWrapper Soundbox;

            public SoundboxTest()
            {
                ServiceProvider = new TestServiceProvider();
                Soundbox = ServiceProvider.GetService(typeof(SoundboxTestWrapper)) as SoundboxTestWrapper;
            }

            #region "Filenames"
            [TestMethod]
            [DataRow("A", "a")]
            [DataRow("Ä", "a")]
            [DataRow("ä", "a")]
            [DataRow("a", "a")]
            [DataRow("hello world", "hello_world")]
            [DataRow("CON", "_con")]
            [DataRow("abcdefghijklmnopqrstuvwxyz0123456789.-_")]
            [DataRow(@"special_charsø{}:/@\^´", "special_chars_________")]
            [DataRow("../test", ".._test")]
            [DataRow(@"..\test", ".._test")]
            public void TestNormalizeFileName(string name, string expected = null)
            {
                if (expected == null)
                    expected = name;
                Assert.AreEqual(expected, Soundbox.NormalizeFileName(name));
            }

            /// <summary>
            /// See https://docs.microsoft.com/en-us/windows/win32/fileio/naming-a-file
            /// </summary>
            /// <param name="reservedFileName"></param>
            [TestMethod]
            [DataRow("CON")]
            [DataRow("PRN")]
            [DataRow("AUX")]
            [DataRow("NUL")]
            [DataRow("COM0")]
            [DataRow("COM1")]
            [DataRow("COM2")]
            [DataRow("COM3")]
            [DataRow("COM4")]
            [DataRow("COM5")]
            [DataRow("COM6")]
            [DataRow("COM7")]
            [DataRow("COM8")]
            [DataRow("COM9")]
            [DataRow("LPT0")]
            [DataRow("LPT1")]
            [DataRow("LPT2")]
            [DataRow("LPT3")]
            [DataRow("LPT4")]
            [DataRow("LPT5")]
            [DataRow("LPT6")]
            [DataRow("LPT7")]
            [DataRow("LPT8")]
            [DataRow("LPT9")]
            [DataRow("CON.txt")]
            [DataRow("PRN.txt")]
            [DataRow("AUX.txt")]
            [DataRow("NUL.txt")]
            [DataRow("COM0.txt")]
            [DataRow("COM1.txt")]
            [DataRow("COM2.txt")]
            [DataRow("COM3.txt")]
            [DataRow("COM4.txt")]
            [DataRow("COM5.txt")]
            [DataRow("COM6.txt")]
            [DataRow("COM7.txt")]
            [DataRow("COM8.txt")]
            [DataRow("COM9.txt")]
            [DataRow("LPT0.txt")]
            [DataRow("LPT1.txt")]
            [DataRow("LPT2.txt")]
            [DataRow("LPT3.txt")]
            [DataRow("LPT4.txt")]
            [DataRow("LPT5.txt")]
            [DataRow("LPT6.txt")]
            [DataRow("LPT7.txt")]
            [DataRow("LPT8.txt")]
            [DataRow("LPT9.txt")]
            public void TestFileNameReserved(string reservedFileName)
            {
                reservedFileName = reservedFileName.ToLower();
                Assert.IsTrue(Soundbox.IsFileNameReserved(reservedFileName));
            }

            /// <summary>
            /// Tests that the given file names are changed when calling <see cref="Soundbox.NormalizeFileName(string)"/>
            /// and that they are not <see cref="Soundbox.IsFileNameReserved(string)"/> anymore.
            /// </summary>
            /// <param name="name"></param>
            [TestMethod]
            [DataRow("CON")]
            [DataRow("PRN")]
            [DataRow("AUX")]
            [DataRow("NUL")]
            [DataRow("COM0")]
            [DataRow("COM1")]
            [DataRow("COM2")]
            [DataRow("COM3")]
            [DataRow("COM4")]
            [DataRow("COM5")]
            [DataRow("COM6")]
            [DataRow("COM7")]
            [DataRow("COM8")]
            [DataRow("COM9")]
            [DataRow("LPT0")]
            [DataRow("LPT1")]
            [DataRow("LPT2")]
            [DataRow("LPT3")]
            [DataRow("LPT4")]
            [DataRow("LPT5")]
            [DataRow("LPT6")]
            [DataRow("LPT7")]
            [DataRow("LPT8")]
            [DataRow("LPT9")]
            [DataRow("CON.txt")]
            [DataRow("PRN.txt")]
            [DataRow("AUX.txt")]
            [DataRow("NUL.txt")]
            [DataRow("COM0.txt")]
            [DataRow("COM1.txt")]
            [DataRow("COM2.txt")]
            [DataRow("COM3.txt")]
            [DataRow("COM4.txt")]
            [DataRow("COM5.txt")]
            [DataRow("COM6.txt")]
            [DataRow("COM7.txt")]
            [DataRow("COM8.txt")]
            [DataRow("COM9.txt")]
            [DataRow("LPT0.txt")]
            [DataRow("LPT1.txt")]
            [DataRow("LPT2.txt")]
            [DataRow("LPT3.txt")]
            [DataRow("LPT4.txt")]
            [DataRow("LPT5.txt")]
            [DataRow("LPT6.txt")]
            [DataRow("LPT7.txt")]
            [DataRow("LPT8.txt")]
            [DataRow("LPT9.txt")]
            public void TestNormalizeFileNameNotEqualReserved(string name)
            {
                string normalized = Soundbox.NormalizeFileName(name);
                Assert.IsFalse(normalized.Equals(name, StringComparison.InvariantCultureIgnoreCase));
                Assert.IsFalse(Soundbox.IsFileNameReserved(normalized));
            }

            [TestMethod]
            [DataRow("test.txt", "txt")]
            [DataRow("test", null)]
            [DataRow("", null)]
            [DataRow(null, null)]
            [DataRow("test.txt.mp3", "mp3")]
            [DataRow("test.txt.", null)]
            [DataRow(".test", "test")]
            public void TestGetFileType(string fileName, string extension)
            {
                Assert.AreEqual(extension, Soundbox.GetFileType(fileName));
            }


            [TestMethod]
            [DataRow("test.txt", "test")]
            [DataRow("test", "test")]
            [DataRow("", "")]
            [DataRow(null, null)]
            [DataRow("test.txt.mp3", "test")]
            [DataRow("test.txt.", "test")]
            [DataRow(".test", "")]
            public void TestGetFileNamePure(string fileName, string namePure)
            {
                Assert.AreEqual(namePure, Soundbox.GetFileNamePure(fileName));
            }

            [TestMethod]
            [DataRow("test.mp3")]
            [DataRow("test.wav")]
            [DataRow("test.ogg")]
            [DataRow("test.aac")]
            [DataRow("TEST.MP3")]
            [DataRow("TEST.WAV")]
            [DataRow("TEST.OGG")]
            [DataRow("TEST.AAC")]
            [DataRow("test.jpg.mp3")]
            [DataRow("test💯.mp3")]
            public void TestCheckUploadFileNameOk(string fileName)
            {
                Assert.IsTrue(Soundbox.CheckUploadFileName(fileName));
            }

            [TestMethod]
            [DataRow(null)]
            [DataRow("")]
            [DataRow("    ")]
            [DataRow(".mp3")]
            [DataRow("    .mp3")]
            [DataRow("test.mp4")]
            [DataRow("test.jpg")]
            public void TestCheckUploadFileNameNok(string fileName)
            {
                Assert.IsFalse(Soundbox.CheckUploadFileName(fileName));
            }

            #endregion
        }
    }
}
