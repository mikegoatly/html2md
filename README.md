# Html2md

![Build and test](https://github.com/mikegoatly/html2md/workflows/Build%20and%20test/badge.svg)

Convert an HTML page to markdown, including re-linking and downloading of images.

## Usage as a dotnet tool

``` powershell
dotnet tool install dotnet-html2md -g
```

Usage:

```
html2md --uri|-u <URI> [--uri|-u <URI> [ ... ]] --output|-o <OUTPUT LOCATION>

Options:

--image-output|-i <IMAGE OUTPUT LOCATION>
If no image output location is specified then they will be written to the same folder as the markdown file.

--include-tags|--it|-t <COMMA SEPARATED TAG LIST>
If unspecified the entire body tag will be processed, otherwise only text contained in the specified tags will be processed.

--exclude-tags|--et|-e <COMMA SEPARATED TAG LIST>
Allows for specific tags to be ignored.

--image-path-prefix|--ipp <IMAGE PATH PREFIX>
The prefix to apply to all rendered image URLs - helpful when you're going to be serving images from a different location, relative or absolute.

--default-code-language <LANGUAGE>
The default language to use on code blocks converted from pre tags - defaults to csharp

--code-language-class-map <CLASSNAME:LANGUAGE,CLASSNAME:LANGUAGE,...>
Map between a pre tag's class names and languages. E.g. you might map the class name "sh_csharp" to "csharp" and "sh_powershell" to "powershell".
```

## Usage as a nuget package

``` powershell
Install-Package Html2md.Core
```

### Example

``` csharp

var converter = new MarkdownConverter(new ConversionOptions());

ConversionResult converted = await converter.ConvertAsync("https://goatly.net/some-article");

// Alternatively you can convert multiple pages at once:

ConversionResult converted = await converter.ConvertAsync(
    new[] 
    { 
        "https://goatly.net/some-article",
        "https://goatly.net/some-other-article",
    });

```

`ConvertedDocument` exposes:

- `Documents`: The markdown representations of all the converted pages.
- `Images`: A collection of images referenced in the documents. Each image includes the downloaded raw data as a byte array.

### Options

In `ConversionOptions` you can specify:

- `ImagePathPrefix`: The prefix to apply to all rendered image URLs - helpful when you're going to be serving
images from a different location, relative or absolute.
- `DefaultCodeLanguage`: The default code language to apply to code blocks mapped from `pre` tags.
The default is `csharp`.
- `IncludeTags`: The set of tags to include in the conversion process. If this is empty then all elements will processed.
- `ExcludeTags`: The set of tags to exclude from the conversion process. You can use this if there are certain parts of
a document you don't want translating to markdown, e.g. aside, nav, etc.
- `CodeLanguageClassMap`: A dictionary mapping between class names that can appear on `pre` tags and the language they map to.E.g. you might map the class name "sh_csharp" to "csharp" and "sh_powershell" to "powershell".

## Converted content

### `<em>`

`<em>italic</em>` becomes `*italic*`

### `<strong>`

`<strong>bold</strong>` becomes `**bold**`

### `<img>`

Linked images from the same domain (relative or absolute) are downloaded and returned in the 
`Images` collection of the `ConvertedDocument`. Images from a different domain are not downloaded and the 
urls are left untouched.

With `ConversionOptions.ImagePathPrefix` of `""`:

`<img src="/images/img.png" alt="My image">` becomes `![My image](img.png)`

With `ConversionOptions.ImagePathPrefix` of `"/static/images/"`:

`<img src="/images/img.png" alt="My image">` becomes `![My image](/static/images/img.png)`

### `<a>`

`<a href="https://goatly.net">Some blog</a>` becomes `[Some blog](https://goatly.net)`

If the link is to an image, then the image is downloaded and the link's URL is updated as with images.

### `<p>`

Paragraph tags cause an additional new line to be inserted after the paragraph's text.

`<p>para 1</p><p>para 2</p>` becomes:

``` markdown
para 1

para 2
```

### `<h1>` ... `<h6>`

Header tags get converted to the markdown equivalent:

`<h2>Header 2</h2><h3>Header 3</h3>` becomes:

``` markdown
## Header 2

### Header 3
```

### `<table>`

Tables are converted, though markdown tables are much more limited than HTML tables. 

Where a header row is present in the source it is used as the markdown table header:

``` html
<table>
    <thead>
        <tr>
            <th>Header 1</th>
            <th>Header 2</th>
        </tr>
    </thead>
    <tbody>
        <tr>
            <td>1-1</td>
            <td>1-2</td>
        </tr>
        <tr>
            <td>2-1</td>
            <td>2-2</td>
        </tr>
    </tbody>
</table>
```

Becomes:

``` markdown

| Header 1 | Header 2 |
|-|-|
| 1-1 | 1-2 |
| 2-1 | 2-2 |

```

If no header row is found, the first row of the table is assumed to be the header:

``` html
<table>
    <tbody>
        <tr>
            <td>1-1</td>
            <td>1-2</td>
        </tr>
        <tr>
            <td>2-1</td>
            <td>2-2</td>
        </tr>
    </tbody>
</table>
```

Becomes:

``` markdown

| 1-1 | 1-2 |
|-|-|
| 2-1 | 2-2 |

```

### `<pre>`

`<pre>content</pre>` becomes:

 ```` markdown
 ```
 content
 ```
 ````

However, if the `pre` tag has a `code` class name it will have the `DefaultCodeLanguage` in the `ConversionOptions` applied to it:

`<pre class="code">content</pre>` with a `DefaultCodeLanguage` of `csharp` becomes:

 ```` markdown
 ``` csharp
 content
 ```
 ````

Additionally, if you have configured the `CodeLanguageClassMap` mapping `lang_ps` to `powershell`:

`<pre class="lang_ps">content</pre>` becomes:

 ```` markdown
 ``` powershell
 content
 ```
 ````

As would `<pre class="code lang_ps">content</pre>`, as the class name lookup will be inspected before falling back to the default code language.

### `<ul>`

``` html
<ul>
    <li>Item 1</li>
    <li>Item 2</li>
    <li>Item 3</li>
</ul>
```

becomes:

``` markdown
- Item 1
- Item 2
- Item 3
```

### `<ol>`

``` html
<ol>
    <li>Item 1</li>
    <li>Item 2</li>
    <li>Item 3</li>
</ol>
```

becomes:

``` markdown
1. Item 1
1. Item 2
1. Item 3
```

> *Markdown renders should automatically apply the correct numbering to lists like this.*

