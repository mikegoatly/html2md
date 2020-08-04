# html2md

Convert an HTML page to markdown, including re-linking and downloading of images.

## Example

``` csharp

var converter = new MarkdownConverter(new ConversionOptions());

ConvertedDocument converted = await converter.ConvertAsync("https://goatly.net/some-article");

```

`ConvertedDocument` exposes:

- `Markdown`: The converted markdown content
- `Images`: A collection of images referenced in the document. Each image has a name and the images raw `byte[]` data.

## Options

In `ConversionOptions` you can specify:

- `ImagePathPrefix`: The prefix to apply to all rendered image URLs - helpful when you're going to be serving
images from a different location, relative or absolute.
- `DefaultCodeLanguage`: The default code language to apply to code blocks mapped from `pre` tags.
- `IncludeTags`: The set of tags to include in the conversion process. If this is empty then all elements will processed.
- `ExcludeTags`: The set of tags to exclude from the conversion process. You can use this if there are certain parts of
a document you don't want translating to markdown, e.g. aside, nav, etc.