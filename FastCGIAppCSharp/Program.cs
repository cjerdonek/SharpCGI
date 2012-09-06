using System;
using System.Net;
using System.IO;
using System.Globalization;
using System.Text;

using Core = Microsoft.FSharp.Core;

using log4net;
using log4net.Config;

using FastCGI;

namespace FastCGIApp
{
    class Program
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Program));
        private static readonly ILog flog = LogManager.GetLogger("FastCGI");

        static void HandleRequest(Request request, Response response)
        {
            log.Debug("Handling request...");

            // receive HTTP content
            byte[] content = request.Stdin.GetContents();

            // access server variables
            string serverSoftware = request.ServerSoftware.GetValueOrDefault();
            string method = request.RequestMethod.Value;

            // access HTTP headers
            string userAgent = request.Headers[RequestHeader.HttpUserAgent];
            string cookieValue = request.GetCookieValue("Keks").GetValueOrDefault();

            // set HTTP headers
            response.SetHeader(ResponseHeader.HttpExpires,
                               Response.ToHttpDate(DateTime.Now.AddDays(1.0)));
            response.SetCookie(new Cookie("Keks", "yummy"));

            // send HTTP content
            response.PutStr(
                @"<html>
                   <body>
                    <p>Hello World!</p>
                    <p>Server: " + serverSoftware + @"</p>
                    <p>User Agent: " + userAgent + @"</p>
                    <p>Received cookie value: " + cookieValue + @"</p>
                    <p>Content length as read: " + content.Length + @"</P>
                    <p>Request method: " + method + @"</p>
                   </body>
                  </html>"
                );
        }

        // Crazy code to convert from C# delegates to F# functions.
        // For example, see: http://blogs.msdn.com/b/jaredpar/archive/2010/07/27/converting-system-func-lt-t1-tn-gt-to-fsharpfunc-lt-t-tresult-gt.aspx

        delegate Core.Unit OnStringDelegate(string s);

        static Converter<string, Core.Unit> MakeConverter(Action<string> onString)
        {
            OnStringDelegate del = (s) => { onString(s); return null; };
            return new Converter<string, Core.Unit>(s => del(s));
        }

        static Core.FSharpFunc<string, Core.Unit> MakeFSharp(Action<string> onString)
        {
            Converter<string, Core.Unit> converter = MakeConverter(onString);
            return Core.FSharpFunc<string, Core.Unit>.FromConverter(converter);
        }

        static void Main(string[] args)
        {
            XmlConfigurator.Configure();

            log.Info("Starting: Main");

            Options config = new Options();
            config.Bind = BindMode.CreateSocket;
            config.EndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9000);

            config.OnError = (s) => { flog.Error(s); };
            config.OnTrace = (s) => { flog.Debug(s); };

            //config.ErrorLogger = MakeFSharp(s => flog.Error(s));
            //config.TraceLogger = MakeFSharp(s => flog.Debug(s));
            
            Server.Start(HandleRequest, config);
        }
    }
}
