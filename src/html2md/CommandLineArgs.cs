using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Html2md
{
    public class CommandLineArgs : IConversionOptions
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
        private readonly string imagePathPrefix = "";
        private readonly string? logLevel = "Error";

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
                    case "--logging":
                        this.SaveArg(args, ref i, ref this.logLevel);
                        break;

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

                    case "--image-path-prefix":
                    case "--ipp":
                        this.SaveArg(args, ref i, ref this.imagePathPrefix!);
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
                        this.SaveArg(args, ref i, ref this.defaultCodeLanguage!);
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

        public LogLevel LogLevel
        {
            get
            {
                if (Enum.TryParse<LogLevel>(this.logLevel, out var level))
                {
                    return level;
                }

                return LogLevel.Error;
            }
        }

        public string ImagePathPrefix => this.imagePathPrefix;

        public string DefaultCodeLanguage => this.defaultCodeLanguage;

        public string? Error { get; set; }

        public bool ShowHelp { get; }

        public string OutputLocation => this.outputLocation;

        public string ImageOutputLocation => this.imageOutputLocation ?? this.outputLocation;

        public string Url => this.url;

        public ISet<string> IncludeTags => this.includeTags;

        public ISet<string> ExcludeTags => this.excludeTags;
    }
}