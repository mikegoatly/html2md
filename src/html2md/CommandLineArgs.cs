using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace html2md
{
    public class CommandLineArgs
    {
        [NotNull]
        private readonly string? outputLocation;

        [NotNull]
        private readonly string? imageOutputLocation;

        [NotNull]
        private readonly string? url;

        private readonly HashSet<string> includeTags = new HashSet<string>(new[] { "body" });
        private readonly HashSet<string> excludeTags = new HashSet<string>();
        private readonly string defaultCodeLanguage = "csharp";

        public CommandLineArgs(string[] args)
        {
            if (args.Length == 0)
            {
                this.ShowHelp = true;
            }

            for (var i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--help":
                        this.ShowHelp = true;
                        break;

                    case "--output":
                    case "-o":
                        this.SaveArg(args, ref i, ref this.outputLocation);
                        break;

                    case "--image-output":
                    case "-i":
                        this.SaveArg(args, ref i, ref this.imageOutputLocation);
                        break;

                    case "--url":
                    case "-u":
                        this.SaveArg(args, ref i, ref this.url);
                        break;

                    case "--include-tags":
                    case "--it":
                    case "-t":
                        this.SaveArg(args, ref i, ref this.includeTags);
                        break;

                    case "--exclude-tags":
                    case "--et":
                    case "-e":
                        this.SaveArg(args, ref i, ref this.excludeTags);
                        break;

                    case "--default-code-language":
                        this.SaveArg(args, ref i, ref this.defaultCodeLanguage);
                        break;

                    default:
                        this.Error = $"Unknown argument {args[i]}";
                        break;
                }
            }

            if (this.outputLocation == null || this.url == null)
            {
                this.ShowHelp = true;
            }
        }

        private void SaveArg(string[] args, ref int i, ref string? arg)
        {
            i += 1;
            if (i >= args.Length)
            {
                this.Error = $"Missing parameter for {args[i - 1]}";
                return;
            }

            arg = args[i];
        }

        private void SaveArg(string[] args, ref int i, ref HashSet<string> arg)
        {
            i += 1;
            if (i >= args.Length)
            {
                this.Error = $"Missing parameter for {args[i - 1]}";
                return;
            }

            arg = args[i].Split(",", StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        }

        public string DefaultCodeLanguage => this.defaultCodeLanguage;

        public string? Error { get; set; }

        public bool ShowHelp { get; }

        public string OutputLocation => this.outputLocation;

        public string ImageOutputLocation => this.imageOutputLocation ?? this.outputLocation;

        public string Url => this.url;

        public HashSet<string> IncludeTags => this.includeTags;

        public HashSet<string> ExcludeTags => this.excludeTags;
    }
}