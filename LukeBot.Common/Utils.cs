using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;


namespace LukeBot.Common
{
    public class Utils
    {
        public static string HttpStatusCodeToHTTPString(HttpStatusCode code)
        {
            // TODO not all codes are filled in cause I'm lazy. Maybe ArgumentException is thrown
            // because I was code is not on the list below. Fill it in some day.
            switch (code)
            {
            // 100s
            case HttpStatusCode.Continue: return "100 Continue";
            case HttpStatusCode.SwitchingProtocols: return "101 Switching Protocols";
            case HttpStatusCode.Processing: return "102 Processing";
            case HttpStatusCode.EarlyHints: return "103 Early Hints";
            // 200s
            case HttpStatusCode.OK: return "200 OK";
            case HttpStatusCode.Created: return "201 Created";
            case HttpStatusCode.Accepted: return "202 Accepted";
            case HttpStatusCode.NonAuthoritativeInformation: return "203 Non-Authoritative Information";
            case HttpStatusCode.NoContent: return "204 No Content";
            // 300s
            // 400s
            case HttpStatusCode.BadRequest: return "400 Bad Request";
            case HttpStatusCode.Unauthorized: return "401 Unauthorized";
            case HttpStatusCode.PaymentRequired: return "402 Payment Required";
            case HttpStatusCode.Forbidden: return "403 Forbidden";
            case HttpStatusCode.NotFound: return "404 Not Found";
            case HttpStatusCode.RequestTimeout: return "408 Request Timeout";
            case HttpStatusCode.Gone: return "410 Gone";
            // 500s
            case HttpStatusCode.InternalServerError: return "500 Internal Server Error";
            case HttpStatusCode.NotImplemented: return "501 Not Implemented";
            case HttpStatusCode.BadGateway: return "502 Bad Gateway";
            case HttpStatusCode.ServiceUnavailable: return "503 Service Unavailable";
            case HttpStatusCode.HttpVersionNotSupported: return "505 HTTP Version Not Supported";
            default:
                throw new ArgumentException(string.Format("Unsupported HTTP status code: {0}", code));
            }
        }

        public static Process StartBrowser(string url)
        {
            Process result = null;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                result = Process.Start("xdg-open", url);
            }
            else
            {
                throw new UnsupportedPlatformException("Platform is not supported");
            }

            return result;
        }

        // Parse a list of strings into a list of key-value tuples. Useful for providing arguments
        // to inner systems of LukeBot (ex. EventSystem's test command, or Widget's config update)
        // Notable parsing details:
        //  - Key always has to be a string without spaces
        //  - There must be no spaces surrounding the = sign, so always <key>=<value>
        //  - Longer strings with spaces are allowed if put in quotation marks
        //  - No escape characters are supported (yet) (TODO?)
        // Following args list is valid:
        //  Tier=2 Message="This is a message!" User=username
        // Produces three tuples (all strings):
        //  ("Tier", "2")
        //  ("Message", "This is a message!")
        //  ("User", "username")
        // TestEvent() will further parse the data for correctness against Event's
        // TestArgs list, if available.
        public static IEnumerable<(string attrib, string value)> ConvertArgStringsToTuples(IEnumerable<string> argsList)
        {
            List<(string attrib, string value)> ret = new();

            string a = "", v = "";
            bool readingString = false;
            foreach (string s in argsList)
            {
                if (readingString)
                {
                    if (s.EndsWith('"'))
                    {
                        readingString = false;
                        v += ' ' + s.Substring(0, s.Length - 1);
                        ret.Add((a, v));
                    }
                    else
                    {
                        v += ' ' + s;
                    }

                    continue;
                }

                string[] tokens = s.Split('=');
                if (tokens.Length != 2)
                {
                    throw new ArgumentException("Failed to parse test event attributes");
                }

                a = tokens[0];

                if (tokens[1].StartsWith('"'))
                {
                    v = tokens[1].Substring(1);
                    readingString = true;
                }
                else
                {
                    v = tokens[1];
                    ret.Add((a, v));
                }
            }

            return ret;
        }
    }
}
