using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Hosting;

namespace Cityline.WebTest.Features.Webpack
{
    public class Parser 
    {
        private readonly string _root;

        public Parser(IWebHostEnvironment env) 
        {
            _root = env.ContentRootPath;
        }

        private HtmlDocument GetDocument(string relativePath) 
        {
            var file = new FileInfo(Path.Combine(_root, relativePath));

            var document = new HtmlDocument();

            using var stream = file.OpenRead();
            document.Load(stream);

            return document;
        }

        public IEnumerable<string> GetScripts(string relativePath) 
        {
            var document = GetDocument(relativePath);

            var scripts = document.DocumentNode.SelectNodes("//script");
            return scripts?.Select(script => script.OuterHtml) ?? Enumerable.Empty<string>();
        }

        public IEnumerable<string> GetScriptSrcs(string relativePath) 
        {
            var document = GetDocument(relativePath);

            var scripts = document.DocumentNode.SelectNodes("//script");
            return scripts?.Select(script => script.Attributes["src"].Value) ?? Enumerable.Empty<string>();
        }

        public IEnumerable<string> GetStyles(string relativePath) 
        {
            var document = GetDocument(relativePath);
            
            var styles = document.DocumentNode.SelectNodes("//link");
            return styles?.Select(script => script.OuterHtml) ?? Enumerable.Empty<string>();
        }

           public IEnumerable<string> GetStyleSrcs(string relativePath) 
        {
            var document = GetDocument(relativePath);
            
            var styles = document.DocumentNode.SelectNodes("//link");
            return styles?.Select(script => script.Attributes["href"].Value) ?? Enumerable.Empty<string>();
        }

         public async Task<string> GetText(string relativePath) 
        {
            var file = new FileInfo(Path.Combine(_root, relativePath));

            using var reader = file.OpenText();

            return await reader.ReadToEndAsync();
        }
    }
}