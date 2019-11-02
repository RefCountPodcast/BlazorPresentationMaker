using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace PresentationMaker.Blazor.Interpreter
{
    public class Interpreter
    {
        private MetadataReference[] _assemblies;
        private HttpClient _client;
        public Interpreter(HttpClient client)
        {
            _client = client;
        }

        public async Task<string> Run(string script)
        {
            var oldOut = Console.Out;
            try
            {
                if (_assemblies == null)
                {
                    var assemblyUrls = new[] {
                        $"_framework/_bin/{typeof(object).Assembly.Location}",
                        $"_framework/_bin/{typeof(System.Linq.Enumerable).Assembly.Location}" };

                    foreach (var url in assemblyUrls)
                    {
                        oldOut.WriteLine(url);
                    }

                    var assemblyResponses = await Task.WhenAll(
                        assemblyUrls.Select(url => _client.GetAsync(url)));

                    var assemblies = await Task.WhenAll(
                        assemblyResponses.Select(resp => resp.Content.ReadAsStreamAsync()));

                    _assemblies = assemblies
                        .Select(a => MetadataReference.CreateFromStream(a))
                        .ToArray();
                }

                oldOut.WriteLine("fetched assemblies");

                var stdOut = new StringWriter();
                Console.SetOut(stdOut);

                var options = ScriptOptions.Default.WithReferences(_assemblies);

                oldOut.WriteLine("Created Options");

/*
                var t = CSharpScript
                        .Create(@"using System;
                                  using System.Collections.Generic;
                                  using System.Linq;",
                                options)
                        .ContinueWith(script)
                        .RunAsync();
*/

                var t = CSharpScript.EvaluateAsync(script, options);

                for (int i=0; i<10; i++) {
                    oldOut.WriteLine($"t.Status = {t.Status}");
                    await Task.Delay(100);
                }

                oldOut.WriteLine("Ran Script");

                return stdOut.ToString();
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
            finally
            {
                Console.SetOut(oldOut);
            }
        }
    }
}