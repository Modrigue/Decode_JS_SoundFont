using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace decode_js_soundfont
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                Console.WriteLine($"Usage: .\\decode_js_soundfont.exe [files or directories paths]");
                return;
            }

            foreach (string dirFile in args)
            {
                if (File.Exists(dirFile))
                {
                    // process file
                    processFile(dirFile);
                }
                else if (Directory.Exists(dirFile))
                {
                    // process all files in folder
                    string[] files = Directory.GetFiles(dirFile);
                    foreach (string filePath in files)
                        processFile(filePath);
                }
                else
                {
                    Console.WriteLine($"File or directory {dirFile} not found");
                }
            }
        }

        private static void processFile(string filePath)
        {
            try
            {
                Console.WriteLine($"Processing {filePath}...");

                // get notes text
                string textNotes = File.ReadAllText(filePath);
                textNotes = removeLineEndings(textNotes);

                Regex patternNotes = new Regex(@".*MIDI.Soundfont.(?<fontName>.+) = {(?<jsonBase64>.*)}");
                Match matchNotes = patternNotes.Match(textNotes);
                if (matchNotes == null)
                {
                    Console.WriteLine($"Soundfont notes not found in file {filePath}");
                    return;
                }

                string fontName = matchNotes.Groups["fontName"].Value;
                string jsonBase64 = matchNotes.Groups["jsonBase64"].Value;

                String[] jsonBase64Strings = jsonBase64.Split('"');

                // parse notes
                List<Note> notesList = new List<Note>();
                string curNoteName = String.Empty;
                Regex patternNoteName = new Regex(@"^[A-G](b?)[0-8]$");
                Regex patternNoteData = new Regex(@"data:audio.*");
                foreach (String text in jsonBase64Strings)
                {
                    // check if text is note name or data

                    Match matchNoteName = patternNoteName.Match(text);
                    if (matchNoteName.Success)
                        curNoteName = text;

                    Match matchNoteData = patternNoteData.Match(text);
                    if (matchNoteData.Success)
                    {
                        Note curNote = new Note(curNoteName, text);
                        notesList.Add(curNote);
                    }
                }

                // save all notes to files

                string dirPath = Path.GetDirectoryName(filePath);
                dirPath = Path.Combine(dirPath, fontName);
                if (!Directory.Exists(dirPath))
                    Directory.CreateDirectory(dirPath);

                foreach (Note note in notesList)
                    note.Save(dirPath);
            }
            catch(Exception)
            {
                Console.WriteLine($"Failed to process file {filePath}");
            }
        }

        // from https://stackoverflow.com/questions/6750116/how-to-eliminate-all-line-breaks-in-string
        private static string removeLineEndings(string value)
        {
            if (String.IsNullOrEmpty(value))
                return value;

            string lineSeparator = ((char)0x2028).ToString();
            string paragraphSeparator = ((char)0x2029).ToString();

            return value.Replace("\r\n", string.Empty)
                        .Replace("\n", string.Empty)
                        .Replace("\r", string.Empty)
                        .Replace(lineSeparator, string.Empty)
                        .Replace(paragraphSeparator, string.Empty);
        }
    }
}
