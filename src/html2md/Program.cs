using System;

namespace html2md
{
    class Program
    {
        static void Main(string[] args)
        {
            var commandLine = new CommandLineArgs(args);
            if (commandLine.ShowHelp || commandLine.Error != null)
            {
                if (commandLine.Error != null)
                {
                    ShowError(commandLine.Error);
                }

                WriteHelp();
            }
            else
            {
                Console.WriteLine("Doing it");
            }

        }

        private static void ShowError(string error)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(error);
            Console.WriteLine();
            Console.ResetColor();
        }

        private static void WriteHelp()
        {
            Console.WriteLine("html2md - Convert  HTML pages to markdown format");
            Console.WriteLine("-----------------");
            Console.WriteLine("html2md --uri|-u <URI> --output|-o <OUTPUT LOCATION>");
            Console.WriteLine("Options:");
            Console.WriteLine("--image-output|-i <IMAGE OUTPUT LOCATION>");
            Console.WriteLine("    If no image output location is specified then they will be written to the same folder as the markdown file.");
        }
    }
}
