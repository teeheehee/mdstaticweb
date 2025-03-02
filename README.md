# mdstaticweb
Static website generator based on markdown files for content in Pages and blog Posts.

## Background
In the first iteration of this project built a site using C# and [LINQPad][LINQPad link] which allowed for a somewhat experimental approach and supported prototyping the structure and process.

The project is just to the point where it is being converted to a .NET solution.

# Current features

* HTML templates
    * replacement tags
* static content copy
* SQLite3
    * website configuration
    * default replacement tag values
    * organizing pages and posts for processing
* Markdown YAML support (limited)
    * HTML template matching by `Type` attribute
    * `Title` attribute applied to `<head>` several HTML tags
    * navigation links for `Page`s with link ordering
* Markdown rendering to HTML
* sitemap.xml file generation for Pages
* blog.rss RSS file generation for Posts

# Technologies so far

* C#
    * [YamlDotNet][YamlDotNet link]
    * [Markdig][Markdig link]
    * [System.Data.SQLite][System.Data.SQLite link]
* [LINQPad][LINQPad link]
* [VisualStudio Code][VisualStudio Code link]
* [SQLite3][SQLite link]
    * [SQLite Browser][SQLite Browser link]
* [AngleSharp][AngleSharp link] library
* [Markdig][Markdig extensions code link] markdown library

# Reference and research links

These are things I had tabs open in my browser while doing bits and pieces of the work for the site so far, besides the ones mentioned above.

* [favicon standards in 2023 on stack overflow](https://stackoverflow.com/questions/48956465/favicon-standard-2023-svg-ico-png-and-dimensions/48969053#48969053)
* [Dan's tools RegEx tester](https://www.regextester.com) (with the search string `\${(.*?)}`)
* [searching for files recursively in C# on stack overflow](https://stackoverflow.com/questions/9830069/searching-for-file-in-directories-recursively)
* [MSDN C# docs for reading/writing text files](https://learn.microsoft.com/en-us/troubleshoot/developer/visualstudio/csharp/language-compilers/read-write-text-file)
* [MSDN C# docs for using RegEx](https://learn.microsoft.com/en-us/dotnet/api/system.text.regularexpressions.regex?view=net-8.0)
* [MSDN C# docs for getting file info](https://learn.microsoft.com/en-us/dotnet/api/system.io.fileinfo?view=net-8.0)
* [C# example for querying a SQLite db](https://csharp.hotexamples.com/examples/System.Data.SQLite/SQLiteDataReader/-/php-sqlitedatareader-class-examples.html)
* [C#/LINQ `ToDictionary()` examples](https://dotnettutorials.net/lesson/todictionary-method/)
* [C#/LINQ `ToDictionary()` example on stack overflow](https://stackoverflow.com/questions/2968356/linq-transform-dictionarykey-value-to-dictionaryvalue-key)
* [C# find text in string on stack overflow](https://stackoverflow.com/questions/10709821/find-text-in-string-with-c-sharp)
* [C# how to strip `Environment.NewLine` from strings on stack overflow](https://stackoverflow.com/questions/30824533/how-to-split-environment-newline)
* [info and examples of robots.txt](https://moz.com/learn/seo/robotstxt)
* [info and examples of sitemap.xml](https://pagedart.com/blog/robots-txt-file-example/)
* [Sitemaps on Wikipedia](https://en.wikipedia.org/wiki/Sitemaps)
* [robots.txt and sitemaps on stack overflow](https://stackoverflow.com/questions/63542354/readthedocs-robots-txt-and-sitemap-xml)
* [SQLite docs for upsert command](https://www.sqlite.org/lang_UPSERT.html)
* [MSDN C# docs for `Microsoft.Data.Sqlite` and parameters](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/parameters)
* [SQLite docs for table constraints](https://www.sqlite.org/syntax/column-constraint.html)
* [factory (pattern)][factory pattern link]

[LINQPad link]: https://www.linqpad.net
[VisualStudio Code link]: https://code.visualstudio.com
[YamlDotNet link]: https://github.com/aaubry/YamlDotNet/wiki
[Markdig link]: https://github.com/xoofx/markdig
[System.Data.SQLite link]: https://system.data.sqlite.org/index.html/doc/trunk/www/index.wiki
[SQLite link]: https://www.sqlite.org/index.html
[SQLite Browser link]: sqlitebrowser.org/
[AngleSharp link]: https://github.com/AngleSharp/AngleSharp/
[Markdig extensions code link]: https://github.com/xoofx/markdig/tree/master/src/Markdig/Extensions
[figure HTML tag documentation link]: https://developer.mozilla.org/en-US/docs/Web/HTML/Element/figure
[factory pattern link]: https://dotnettutorials.net/lesson/factory-design-pattern-csharp/