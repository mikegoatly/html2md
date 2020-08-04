using FluentAssertions;
using System.Collections.Generic;
using Xunit;

namespace Html2md.Tests.Unit
{
    public class CommandLineArgsTests
    {
        [Fact]
        public void WithNoArguments_ShouldShowHelp()
        {
            var sut = new CommandLineArgs(new string[0]);
            sut.ShowHelp.Should().BeTrue();
        }

        [Fact]
        public void WithFullArgumentNames_ShouldSetValuesCorrectly()
        {
            var sut = new CommandLineArgs(new[] {
                "--output",
                "c:\\test\\output",
                "--url",
                "http://goatly.net",
                "--image-output",
                "c:\\test\\images",
                "--code-language-class-map",
                "cs:csharp,ps:powershell"
            });

            sut.Error.Should().BeNull();
            sut.OutputLocation.Should().Be("c:\\test\\output");
            sut.Url.Should().Be("http://goatly.net");
            sut.ImageOutputLocation.Should().Be("c:\\test\\images");
            sut.ShowHelp.Should().BeFalse();
            sut.CodeLanguageClassMap.Should().BeEquivalentTo(
                new Dictionary<string, string>
                {
                    { "cs", "csharp" },
                    { "ps", "powershell" }
                });
        }

        [Fact]
        public void WithAbbreviatedArgumentNames_ShouldSetValuesCorrectly()
        {
            var sut = new CommandLineArgs(new[] {
                "-o",
                "c:\\test\\output",
                "-u",
                "http://goatly.net",
                "-i",
                "c:\\test\\images",
            });

            sut.OutputLocation.Should().Be("c:\\test\\output");
            sut.Url.Should().Be("http://goatly.net");
            sut.ImageOutputLocation.Should().Be("c:\\test\\images");
            sut.ShowHelp.Should().BeFalse();
        }

        [Fact]
        public void WithHelpArgument_ShouldSetShowHelpProperty()
        {
            var sut = new CommandLineArgs(new[] {
                "-o",
                "c:\\test\\output",
                "-u",
                "http://goatly.net",
                "--help",
            });

            sut.ShowHelp.Should().BeTrue();
        }

        [Fact]
        public void WithNoImageOutputLocation_ShouldUseOutputLocation()
        {
            var sut = new CommandLineArgs(new[] {
                "-o",
                "c:\\test\\output",
                "-u",
                "http://goatly.net"
            });

            sut.ImageOutputLocation.Should().Be("c:\\test\\output");
        }

        [Fact]
        public void WithUnknownArgument_ShouldReportErrorAndShowHelp()
        {
            var sut = new CommandLineArgs(new[] {
                "-z"
            });

            sut.Error.Should().Be("Unknown argument -z");
            sut.ShowHelp.Should().BeTrue();
        }

        [Fact]
        public void WithMissingArgumentValue_ShouldReportErrorAndShowHelp()
        {
            var sut = new CommandLineArgs(new[] {
                "-o"
            });

            sut.Error.Should().Be("Missing parameter for -o");
            sut.ShowHelp.Should().BeTrue();
        }
    }
}
