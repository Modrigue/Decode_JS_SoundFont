using System;
using System.IO;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

namespace decode_js_soundfont
{
    public class Note
    {

        public string Name { get; set; }
        
        public string Data { get; set; }

        public string Format { get; set; }

        public string ValueBase64 { get; set; }

        public byte[] Value { get; }

        public Note(string name, string data)
        {
            this.Name = name;
            this.Data = data;

            // parse data
            Regex pattern = new Regex(@"^data:audio\/(?<format>.+);base64,(?<valueBase64>.+)$");
            Match match = pattern.Match(data);
            if (match.Success)
            {
                this.Format = match.Groups["format"].Value;
                this.ValueBase64 = match.Groups["valueBase64"].Value;

                this.Value = Convert.FromBase64String(this.ValueBase64);
            }
        }

        // save note audio to file
        public void Save(string dirPath)
        {
            string filePath = Path.Combine(dirPath, $"{this.Name}.{this.Format}");
            File.WriteAllBytes(filePath, this.Value);
        }
    }
}
