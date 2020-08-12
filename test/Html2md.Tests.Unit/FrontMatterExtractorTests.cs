using FluentAssertions;
using HtmlAgilityPack;
using System;
using Xunit;

namespace Html2md.Tests.Unit
{
    public class FrontMatterExtractorTests
    {
        private static readonly string testPage = @"<html>
<head>
    <meta content='Orchard' name='generator' />
</head>
<body>
<article class='blog-post content-item'>
    <header>
        

<h1>Adding Application Insights to an existing Windows Store project using ""Visual Studio 2013 Update 3""</h1>

    <p class='tags'>
        <span>Tags:</span>
<a href = '/Tags/Application%20Insights' > Application Insights</a>, <a href = '/Tags/WinRT'> WinRT </a>, <a href = '/Tags/Visual%20Studio' > Visual Studio</a>    </p>

            <div class='metadata'>
                <div class='published'>Thursday, August 7, 2014 11:55:08 AM</div>
            </div>
    </header>
    <p>As of Update 3, Visual Studio 2013 now has support for Application Insights built in, so I thought I’d have a play with it again.</p> <p>Right now, my primary focus is adding instrumentation to a<a href='http://www.chordle.com'>Windows 8 Store app I’m working on</a>.I’d tried to do it with the previous Application Insights release (which was in preview), but found the need to explicitly build the app for all the various CPU targets a burden I could do without.The release that comes with Update 3 would seem to have fixed this, by allowing you to target Any CPU.</ p > < p > This is a summary of my experience of adding Application Insights to my existing real - world project.</ p > < p > First I removed any indication that I had ever had Application Insights added to the store project by removing the ApplicationInsights.config file that was already there from earlier attempts.</ p > < p > Then I right - clicked on the project, and selected Add Application Insights – on doing this, I received the following error:</ p > < blockquote > < p > Could not add Application Insights to project.& nbsp; </p> <p>Failed to install package: </p> <p>Microsoft.ApplicationInsights.WindowsStore</p> <p>with error: </p> <p>An error occurred while applying transformation to 'App.xaml' in project '&lt;PROJECT&gt;: No element in the source document matches '/_defaultNamespace:Application/_defaultNamespace:Application.Resources'</p></blockquote> <p>It turns out that the installer didn’t like the fact that my application had an unexpected App.xaml structure, due to the use of Prism as the application framework:</p><pre class='xml'>&lt;prism:MvvmAppBase x:Class='Chordle.UI.App'<br>                   xmlns=http://schemas.microsoft.com/winfx/2006/xaml/presentation<br>                   xmlns:x=http://schemas.microsoft.com/winfx/2006/xaml<br>                   xmlns:prism='using:Microsoft.Practices.Prism.StoreApps'&gt;<br>&nbsp;&nbsp;&nbsp; &lt;prism:MvvmAppBase.Resources&gt;<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; &lt;ResourceDictionary&gt;
…<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; &lt;/ResourceDictionary&gt;<br>&nbsp;&nbsp;&nbsp; &lt;/prism:MvvmAppBase.Resources&gt;<br>&lt;/prism:MvvmAppBase&gt;</pre>
<p>So to get around this, I had to comment out my existing XAML and add in a temporary Application.Resources area, like this:<br></p><pre class='xml'>&lt;!--&lt;prism:MvvmAppBase x:Class='Chordle.UI.App'<br>                   xmlns=http://schemas.microsoft.com/winfx/2006/xaml/presentation<br>                   xmlns:x=http://schemas.microsoft.com/winfx/2006/xaml<br>                   xmlns:prism='using:Microsoft.Practices.Prism.StoreApps'&gt;<br>&nbsp;&nbsp;&nbsp; &lt;prism:MvvmAppBase.Resources&gt;<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; &lt;ResourceDictionary&gt;
…<br>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; &lt;/ResourceDictionary&gt;<br>&nbsp;&nbsp;&nbsp; &lt;/prism:MvvmAppBase.Resources&gt;<br>&lt;/prism:MvvmAppBase&gt;—-&gt;<br>
&lt;xaml:Application xmlns:xaml='http://schemas.microsoft.com/winfx/2006/xaml/presentation'&gt;<br>    &lt;xaml:Application.Resources /&gt;<br>&lt;/xaml:Application&gt;</pre>
<p>And after closing App.xaml, I tried to add App Insights again, this time it succeeded, but I obviously had to fix-up the App.xaml file by uncommenting the original XAML, and moving the new ai:TelemetryContext resource into my own resource dictionary structure.</p>
<p>After all this, I finally discovered that currently you can’t yet view Windows Store/Phone telemetry in the preview Azure Portal, which is where the telemetry is going now, so there’s no way for me to test our whether this has actually worked… I’ll write another post when I’ve got more to add!</p>

<h2 class='comment-count'>&quot;No Comments&quot;</h2>

</article></div>
</body>
</html>
".Replace('\'', '"');

        private static readonly Uri testPageUri = new Uri("http://goatly.net/2012/03/some-post");

        [Fact]
        public void WhenDisabledShouldReturnNull()
        {
            this.RunFrontMatterTest(
                new FrontMatterOptions() { Enabled = false },
                testPage,
                testPageUri,
                null);
        }

        [Fact]
        public void ShouldMapInnerTextForSingleProperties()
        {
            this.RunFrontMatterTest(
                new FrontMatterOptions()
                {
                    Enabled = true,
                    SingleValueProperties =
                    {
                        { "title", new PropertyMatchExpression("//article/header/h1") },
                        { "firsttag", new PropertyMatchExpression("//p[@class='tags']/a") }
                    }
                },
                testPage,
                testPageUri,
                @"---
title: ""Adding Application Insights to an existing Windows Store project using \""Visual Studio 2013 Update 3\""""
firsttag: ""Application Insights""
---
");
        }

        [Fact]
        public void ShouldNotMapValueIfElementNotFound()
        {
            this.RunFrontMatterTest(
                new FrontMatterOptions()
                {
                    Enabled = true,
                    SingleValueProperties =
                    {
                        { "generator", new PropertyMatchExpression("//article/header/h9") }
                    }
                },
                testPage,
                testPageUri,
                @"---
---
");
        }

        [Fact]
        public void ShouldNotMapArrayIfElementsNotFound()
        {
            this.RunFrontMatterTest(
                new FrontMatterOptions()
                {
                    Enabled = true,
                    ArrayValueProperties =
                    {
                        { "generator", new PropertyMatchExpression("//article/header/h9") }
                    }
                },
                testPage,
                testPageUri,
                @"---
---
");
        }

        [Fact]
        public void ShouldMapAttributeValuesForSingleProperties()
        {
            this.RunFrontMatterTest(
                new FrontMatterOptions()
                {
                    Enabled = true,
                    SingleValueProperties =
                    {
                        { "generator", new PropertyMatchExpression("/html/head/meta[@name='generator']/@content") }
                    }
                },
                testPage,
                testPageUri,
                @"---
generator: ""Orchard""
---
");
        }

        [Fact]
        public void ShouldReturnConstantMacroValue()
        {
            this.RunFrontMatterTest(
                new FrontMatterOptions()
                {
                    Enabled = true,
                    SingleValueProperties =
                    {
                        { "author", new PropertyMatchExpression("{{'Mike Goatly'}}") }
                    }
                },
                testPage,
                testPageUri,
                @"---
author: ""Mike Goatly""
---
");
        }

        [Fact]
        public void ShouldExpandMacroValues()
        {
            this.RunFrontMatterTest(
                new FrontMatterOptions()
                {
                    Enabled = true,
                    SingleValueProperties =
                    {
                        { "RedirectFrom", new PropertyMatchExpression("{{RelativeUriPath}}") }
                    }
                },
                testPage,
                testPageUri,
                @"---
RedirectFrom: ""/2012/03/some-post""
---
");
        }

        [Fact]
        public void ShouldDecodeHtmlEntities()
        {
            this.RunFrontMatterTest(
                new FrontMatterOptions()
                {
                    Enabled = true,
                    SingleValueProperties =
                    {
                        { "test", new PropertyMatchExpression("//h2[@class='comment-count']") }
                    }
                },
                testPage,
                testPageUri,
                @"---
test: ""\""No Comments\""""
---
");
        }

        [Fact]
        public void ShouldMapArrayValues()
        {
            this.RunFrontMatterTest(
                new FrontMatterOptions()
                {
                    Enabled = true,
                    ArrayValueProperties =
                    {
                        { "Tags", new PropertyMatchExpression("//p[@class='tags']/a") }
                    }
                },
                testPage,
                testPageUri,
                @"---
Tags:
- ""Application Insights""
- ""WinRT""
- ""Visual Studio""
---
");
        }

        private void RunFrontMatterTest(FrontMatterOptions frontMatterOptions, string testPage, Uri pageUri, string expectedResult)
        {
            var document = new HtmlDocument();
            document.LoadHtml(testPage);
            var result = new FrontMatterExtractor().Extract(frontMatterOptions, document, pageUri);

            result.Should().Be(expectedResult);
        }
    }
}
