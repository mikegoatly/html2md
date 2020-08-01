using System.Diagnostics.CodeAnalysis;

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

        private readonly bool showHelp;

        public CommandLineArgs(string[] args)
        {
            if (args.Length == 0)
            {
                this.showHelp = true;
            }

            for (var i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--help":
                        this.showHelp = true;
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

                    default:
                        this.Error = $"Unknown argument {args[i]}";
                        break;
                }
            }

            if (this.outputLocation == null || this.url == null)
            {
                this.showHelp = true;
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

        public string? Error { get; set; }

        public bool ShowHelp => this.showHelp;

        public string OutputLocation => this.outputLocation;

        public string ImageOutputLocation => this.imageOutputLocation ?? this.outputLocation;

        public string Url => this.url;
    }
}