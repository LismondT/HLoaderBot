using System.Globalization;
using System.IO;

namespace HLoaderBot
{
    internal class Logger
    {
        static string FILE_PATH => "./data/Logs.txt";

        public static void Log(string message, ConsoleColor color, bool writeInFile = true)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
            
            if (writeInFile)
            {
                WriteInFile(message);
            }
        }

        private static void WriteInFile(string message)
        {
            if (!File.Exists(FILE_PATH))
            {
                FileStream fs = File.Create(FILE_PATH);
                fs.Close();
            }

            message = message.Replace("\n\t", " ");
            message = message.Replace("\n", "");
            string dateTime = DateTime.Now.ToString();
            string toWrite = $"|{dateTime}|=> {message}";

            if (message.Contains("@{date}"))
            {
                toWrite = message.Replace("@{date}", dateTime);
            }

            using (StreamWriter writer = new StreamWriter(FILE_PATH, true))
            {
                writer.WriteLineAsync(toWrite);
            }
        }
    }
}
