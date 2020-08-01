using System;
using System.Threading.Tasks;

namespace html2md
{
    class Program
    {
        static async Task Main(string[] args)
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
                var converter = new Converter(commandLine);
                await converter.ExecuteAsync();
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
            Console.WriteLine("--include-tags|--it|-t <COMMA SEPARATED TAG LIST>");
            Console.WriteLine("    If unspecified the entire body tag will be processed, otherwise only text contained in the specified tags will be processed.");
            Console.WriteLine("--exclude-tags|--et|-e <COMMA SEPARATED TAG LIST>");
            Console.WriteLine("    Allows for specific tags to be ignored. When combined with --include-tags, the excluded tag list will only be applied to tags nested within included tags.");                
        }
    }
}
