namespace DemoBotApp
{
    using System.IO;
    using System.Web.Hosting;

    public class SampleMusic
    {
        private static MemoryStream audioStream = new MemoryStream();
        private static byte[] audioBytes;

        static SampleMusic()
        {
            audioStream = new MemoryStream();
            string sampleMusicFilePath = HostingEnvironment.MapPath("~/musicsample.wav");
            //string sampleMusicFilePath = @" D:\home\site\wwwroot\musicsample.wav";
            if (File.Exists(sampleMusicFilePath))
            {
                audioBytes = File.ReadAllBytes(sampleMusicFilePath);
            }           
        }

        public static Stream GetStream()
        {
            MemoryStream ms = new MemoryStream();
            ms.Write(audioBytes, 0, audioBytes.Length);
            ms.Position = 0;
            return ms;
        }

        ~SampleMusic()
        {
            audioStream.Dispose();
        }
    }
}