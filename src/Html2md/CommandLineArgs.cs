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

        private readonly List<Uri> uris = new List<Uri>();

        private readonly HashSet<string> includeTags = new HashSet<string>(new[] { "body" });
        private readonly HashSet<string> excludeTags = new HashSet<string>();
        private readonly Dictionary<string, string> codeLanguageClassMap = new Dictionary<string, string>();
        private readonly string defaultCodeLanguage = "csharp";
        private readonly string imagePathPrefix = "";
        private readonly string? logLevel = "Error";

        public CommandLineArgs(string[] args)
        {
            if (args.Length == 0)
            {
                this.ShowHelp = true;
            }

            for (var i = 0; i < args.Length && this.Error == null; i++)
            {
                switch (args[i])
                {
                    case "--logging":
                        SaveArg(args, ref i, ref this.logLevel);
                        break;

                    case "--help":
                        this.ShowHelp = true;
                        break;

                    case "--output":
                    case "-o":
                        SaveArg(args, ref i, ref this.outputLocation);
                        break;

                    case "--image-output":
                    case "-i":
                        SaveArg(args, ref i, ref this.imageOutputLocation);
                        break;

                    case "--front-matter-data":
                        AddFrontMatterPropertyArg(args, ref i, this.FrontMatter.SingleValueProperties);
                        this.FrontMatter.Enabled = true;
                        break;

                    case "--front-matter-data-list":
                        AddFrontMatterPropertyArg(args, ref i, this.FrontMatter.ArrayValueProperties);
                        this.FrontMatter.Enabled = true;
                        break;

                    case "--image-path-prefix":
                    case "--ipp":
                        SaveArg(args, ref i, ref this.imagePathPrefix!);
                        break;

                    case "--url":
                    case "-u":
                        SaveUrlArg(args, ref i);
                        break;

                    case "--include-tags":
                    case "--it":
                    case "-t":
                        AddArg(args, ref i, ref this.includeTags);
                        break;

                    case "--exclude-tags":
                    case "--et":
                    case "-e":
                        AddArg(args, ref i, ref this.excludeTags);
                        break;

                    case "--code-language-class-map":
                        SaveArg(args, ref i, ref this.codeLanguageClassMap!);
                        break;

                    case "--default-code-language":
                        SaveArg(args, ref i, ref this.defaultCodeLanguage!);
                        break;

                    default:
                        this.Error = $"Unknown argument {args[i]}";
                        break;
                }
            }

            if (this.outputLocation == null || this.uris.Count == 0)
            {
                this.ShowHelp = true;
            }
        }

        private string? GetArgParameter(string[] args, ref int i)
        {
            i += 1;
            if (i >= args.Length)
            {
                this.Error = $"Missing parameter for {args[i - 1]}";
                return null;
            }

            return args[i];
        }

        private void SaveUrlArg(string[] args, ref int i)
        {
            var url = GetArgParameter(args, ref i);
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                this.Error = "Invalid URI: " + url;
                return;
            }

            this.uris.Add(uri);
        }

        private void SaveArg(string[] args, ref int i, ref string? arg)
        {
            arg = GetArgParameter(args, ref i);
        }

        private void AddArg(string[] args, ref int i, ref HashSet<string> arg)
        {
            var argValue = GetArgParameter(args, ref i);
            if (argValue != null)
            {
                arg = args[i].Split(",", StringSplitOptions.RemoveEmptyEntries).ToHashSet();
            }
        }

        private void AddFrontMatterPropertyArg(string[] args, ref int i, Dictionary<string, PropertyMatchExpression> arg)
        {
            var argIndex = i;
            var argValue = GetArgParameter(args, ref i);
            if (argValue != null)
            {
                var pair = argValue.Split(":");

                if (pair.Length == 2)
                {
                    arg[pair[0]] = new PropertyMatchExpression(pair[1]);
                }
                else if (pair.Length == 3)
                {
                    if (Enum.TryParse<PropertyDataType>(pair[2], out var dataType))
                    {
                        arg[pair[0]] = new PropertyMatchExpression(pair[1], dataType);
                    }
                    else
                    {
                        this.Error = "Malformed argument value for " + args[argIndex] + " - unsupported data type " + pair[2];
                    }
                }
                else
                { 
                    this.Error = "Malformed argument value for " + args[argIndex];
                }
            }
        }

        private void SaveArg(string[] args, ref int i, ref Dictionary<string, string> arg)
        {
            var argIndex = i;
            var argValue = GetArgParameter(args, ref i);
            if (argValue != null)
            {
                var pairs = args[i].Split(",", StringSplitOptions.RemoveEmptyEntries)
                .Select(kp => kp.Split(":"))
                .ToList();

                if (pairs.Any(p => p.Length != 2))
                {
                    this.Error = "Malformed argument value for " + args[argIndex];
                }
                else
                {
                    arg = pairs.ToDictionary(p => p[0], p => p[1]);
                }
            }
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

        public IReadOnlyList<Uri> Uris => this.uris;

        public ISet<string> IncludeTags => this.includeTags;

        public ISet<string> ExcludeTags => this.excludeTags;

        public IDictionary<string, string> CodeLanguageClassMap => this.codeLanguageClassMap;

        public FrontMatterOptions FrontMatter { get; } = new FrontMatterOptions();
    }
}