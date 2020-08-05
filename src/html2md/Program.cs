using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Html2md
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var commandLine = new CommandLineArgs(args);

            var loggerFactory = LoggerFactory.Create(
                b => b.AddConsole(
                    o =>
                    {
                        o.IncludeScopes = false;
                    })
                    .SetMinimumLevel(commandLine.LogLevel));

            var logger = loggerFactory.CreateLogger("html2md");

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
                var converter = new MarkdownConverter(commandLine, logger);
                Directory.CreateDirectory(commandLine.OutputLocation);
                Directory.CreateDirectory(commandLine.ImageOutputLocation);

                var converted = await converter.ConvertAsync(commandLine.Uris);

                foreach (var document in converted.Documents)
                {
                    var documentFilePath = Path.Combine(commandLine.OutputLocation, document.Name);
                    Console.WriteLine("Writing markdown file " + documentFilePath);
                    await File.WriteAllTextAsync(documentFilePath, document.Markdown);
                }

                foreach (var image in converted.Images)
                {
                    var imagePath = Path.Combine(commandLine.ImageOutputLocation, image.FileName);
                    Console.WriteLine("Writing image file " + imagePath);
                    await File.WriteAllBytesAsync(imagePath, image.Data);
                }
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
            Console.WriteLine("html2md --uri|-u <URI> [--uri|-u <URI> [ ... ]] --output|-o <OUTPUT LOCATION>");
            Console.WriteLine("Options:");
            Console.WriteLine();
            Console.WriteLine("--image-output|-i <IMAGE OUTPUT LOCATION>");
            Console.WriteLine("If no image output location is specified then they will be written to the same folder as the markdown file.");
            Console.WriteLine();
            Console.WriteLine("--include-tags|--it|-t <COMMA SEPARATED TAG LIST>");
            Console.WriteLine("If unspecified the entire body tag will be processed, otherwise only text contained in the specified tags will be processed.");
            Console.WriteLine();
            Console.WriteLine("--exclude-tags|--et|-e <COMMA SEPARATED TAG LIST>");
            Console.WriteLine("Allows for specific tags to be ignored.");
            Console.WriteLine();
            Console.WriteLine("--image-path-prefix|--ipp <IMAGE PATH PREFIX>");
            Console.WriteLine("The prefix to apply to all rendered image URLs - helpful when you're going to be serving images from a different location, relative or absolute.");
            Console.WriteLine();
            Console.WriteLine("--default-code-language <LANGUAGE>");
            Console.WriteLine("The default language to use on code blocks converted from pre tags - defaults to csharp");
            Console.WriteLine();
        }
    }
}
