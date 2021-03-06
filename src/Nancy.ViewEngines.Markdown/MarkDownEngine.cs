namespace Nancy.ViewEngines.Markdown
{
    using Nancy.ViewEngines;
    using System.Collections.Generic;
    using Nancy.Responses;
    using System.IO;
    using MarkdownSharp;
    using Nancy.ViewEngines.SuperSimpleViewEngine;
    using System;
    using System.Text.RegularExpressions;

    public class MarkDownEngine : IViewEngine
    {
        private readonly IRootPathProvider rootPathProvider;
        private readonly SuperSimpleViewEngine engineWrapper;

        public IEnumerable<string> Extensions
        {
            get { return new[] { "md" }; }
        }

        public MarkDownEngine(IRootPathProvider rootPathProvider, SuperSimpleViewEngine engineWrapper)
        {
            this.rootPathProvider = rootPathProvider;
            this.engineWrapper = engineWrapper;
        }

        public void Initialize(ViewEngineStartupContext viewEngineStartupContext)
        {
        }

        public Response RenderView(ViewLocationResult viewLocationResult, dynamic model, IRenderContext renderContext)
        {
            var response = new HtmlResponse();

            var html = renderContext.ViewCache.GetOrAdd(viewLocationResult, result =>
                                                                     {
                                                                         string markDown = File.ReadAllText(rootPathProvider.GetRootPath() + viewLocationResult.Location + Path.DirectorySeparatorChar + viewLocationResult.Name + ".md");
                                                                        
                                                                         MarkdownOptions options = new MarkdownOptions();
                                                                         options.AutoNewLines = false;
                                                                         var parser = new Markdown(options);
                                                                         return parser.Transform(markDown);
                                                                     });

            /*
            
            <p>		- matches the literal string "<p>"
            (		- creates a capture group, so that we can get the text back by backreferencing in our replacement string
            @		- matches the literal string "@"
            [^<]*	- matches any character other than the "<" character and does this any amount of times
            )		- ends the capture group
            </p>	- matches the literal string "</p>"
            
            */

            var regex = new Regex("<p>(@[^<]*)</p>");
            var serverHtml = regex.Replace(html, "$1");

            var renderHtml = this.engineWrapper.Render(serverHtml, model, new MarkdownViewEngineHost(new NancyViewEngineHost(renderContext), renderContext));

            response.Contents = stream =>
            {
                var writer = new StreamWriter(stream);
                writer.Write(renderHtml);
                writer.Flush();
            };

            return response;
        }
    }
}

