using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using LukeBot.API;
using LukeBot.Common;
using LukeBot.Communication;
using LukeBot.Config;
using LukeBot.Logging;
using Intercom = LukeBot.Communication.Common.Intercom;
using LukeBot.Widget.Common;
using Microsoft.Extensions.FileProviders;



namespace LukeBot.Endpoint
{
    public class Startup
    {
        async Task LoadPage(string page, HttpContext context)
        {
            StreamReader reader = File.OpenText("LukeBot.Endpoint/Pages/" + page);
            string p = reader.ReadToEnd();
            reader.Close();
            await context.Response.WriteAsync(p);
        }

        async Task HandleAPICall(string call, HttpContext context)
        {
            string responseString;

            if (call == "Users")
            {
                UsersResponse response = new UsersResponse();
                response.status = 0;
                response.users.Add(new Common.UserItem{
                    name = "lookey",
                    displayName = "Looki",
                });
                response.users.Add(new Common.UserItem{
                    name = "michakes",
                    displayName = "Michie",
                });

                responseString = JsonSerializer.Serialize(response);
            }
            else
            {
                ResponseBase response = new ResponseBase{
                    status = 1,
                };

                responseString = JsonSerializer.Serialize(response);
            }

            await context.Response.WriteAsync(responseString);
        }

        async Task HandleServiceCallback(string service, HttpContext context)
        {
            Logger.Log().Info("Received callback for service " + service + ": " + context.Request.Path.Value);
            Logger.Log().Debug("We have " + context.Request.Query.Count + " queries");
            if (Logger.IsLogLevelEnabled(LogLevel.Secure))
            {
                foreach (var query in context.Request.Query)
                {
                    Logger.Log().Secure("  -> " + query.Key + " = " + query.Value);
                }
            }

            Intermediary srv = Comms.Intermediary.GetIntermediary(service);

            if (!context.Request.Query.ContainsKey("state"))
            {
                Logger.Log().Error("Received back no state. This should not happen.");
                return;
            }

            string state = context.Request.Query["state"];

            if (context.Request.Query.ContainsKey("error"))
            {
                srv.Reject(state);
                await context.Response.WriteAsync(
                    "<html><body style=\"font-family: sans-serif; margin-left: 30px; margin-top: 30px;\">" +
                        "Login to " + service + " rejected: " + context.Request.Query["error_description"] + '\n' +
                    "</body></html>"
                );
                return;
            }

            try
            {
                UserToken token = new UserToken();
                token.code = context.Request.Query["code"];
                token.state = state;

                srv.Fulfill(token.state, token);

                await context.Response.WriteAsync(
                    "<html><body style=\"font-family: sans-serif; margin-left: 30px; margin-top: 30px;\">" +
                        "Login to " + service + " successful, you can close the window now.\n" +
                    "</body></html>"
                );
            }
            catch (System.Exception e)
            {
                Logger.Log().Error("{0}", e.Message);
                srv.Reject(state);
                await context.Response.WriteAsync(
                    "<html><body style=\"font-family: sans-serif; margin-left: 30px; margin-top: 30px;\">" +
                        "Login to " + service + " failed. Check log for details.\n" +
                    "</body></html>"
                );
            }
        }

        async Task HandleWidgetCallback(string widgetUUID, HttpContext context)
        {
            Logger.Log().Debug("Widget requested - handling {0}", widgetUUID);

            GetWidgetPageMessage msg = new GetWidgetPageMessage(widgetUUID);
            GetWidgetPageResponse page =
                Comms.Intercom.Request<GetWidgetPageResponse, GetWidgetPageMessage>(msg);

            page.Wait();

            if (page.Status == Intercom::MessageStatus.SUCCESS)
            {
                await context.Response.WriteAsync(page.pageContents);
            }
            else
            {
                await context.Response.WriteAsync("Couldn't load widget: " + page.ErrorReason);
            }
        }

        async Task HandleWidgetWSCallback(string widgetUUID, HttpContext context)
        {
            Logger.Log().Debug("Widget WS connection requested - handling {0}", widgetUUID);

            if (!context.WebSockets.IsWebSocketRequest)
            {
                Logger.Log().Warning("Connection to WebSocket endpoint is not a WS request! Aborting");
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            try
            {
                WebSocket ws = await context.WebSockets.AcceptWebSocketAsync();

                AssignWSMessage msg = new AssignWSMessage(widgetUUID, ws);
                AssignWSResponse resp =
                    Comms.Intercom.Request<AssignWSResponse, AssignWSMessage>(msg);

                resp.Wait();

                if (resp.Status != Intercom::MessageStatus.SUCCESS)
                {
                    await ws.CloseAsync(WebSocketCloseStatus.InternalServerError,
                        string.Format(resp.ErrorReason.Substring(0, 120)),
                        CancellationToken.None
                    );
                    return;
                }

                Logger.Log().Debug("Awaiting lifetime task to keep connection to {0} Widget WS alive", widgetUUID);
                // await for ws to complete, it will be closed in IWidget.cs when needed
                await resp.lifetimeTask;
                Logger.Log().Debug("Lifetime task for Widget WS {0} finished", widgetUUID);

                // TODO at this point Kestrel logs "the application completed without reading the entire request body."
                // I'm not sure why this happens, probably should be taken care of but I couldn't find any reason why
                // or how to remedy it.
            }
            catch (Exception e)
            {
                Logger.Log().Error("Error while processing WS connection for widgets: {0}", e.Message);
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            }
        }

        public void ConfigureServices(IServiceCollection services)
        {
            string domain = Conf.Get<string>(Common.Constants.PROP_STORE_HTTPS_DOMAIN_PROP);

            // LettuceEncrypt is not needed when domain is set to localhost
            // we assume we're in dev environment which has dev certificate provided
            if (!domain.Contains("localhost"))
            {
                string email = Conf.Get<string>(Common.Constants.PROP_STORE_HTTPS_EMAIL_PROP);

                services.AddLettuceEncrypt(c =>
                {
                    c.AcceptTermsOfService = true;
                    c.DomainNames = new string[] { domain };
                    c.EmailAddress = email;
                });
            }
            else
            {
                Logger.Log().Warning("=== NOTE ===");
                Logger.Log().Warning("HTTPS domain is set to localhost - assuming we're in dev environment");
                Logger.Log().Warning("If something fails, remember to run \"dotnet dev-certs https --trust\"");
                Logger.Log().Warning("============");
            }
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();
            app.UseWebSockets();
            app.UseRouting();
            app.UseStaticFiles(new StaticFileOptions()
            {
                FileProvider = new PhysicalFileProvider(Directory.GetCurrentDirectory() + "/Data/ContentRoot"),
                RequestPath = "/content"
            });
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context => {
                    await LoadPage("index.html", context);
                });
                endpoints.MapGet("css/{stylesheet}", async context => {
                    var stylesheet = context.Request.RouteValues["stylesheet"];
                    await LoadPage($"css/{stylesheet}", context);
                });
                endpoints.MapGet("js/{script}", async context => {
                    var script = context.Request.RouteValues["script"];
                    await LoadPage($"js/{script}", context);
                });
                endpoints.MapGet("views/{view}", async context => {
                    var view = context.Request.RouteValues["view"];
                    await LoadPage($"views/{view}", context);
                });
                endpoints.MapGet("api/{call}", async context => {
                    var call = context.Request.RouteValues["call"];
                    await HandleAPICall($"{call}", context);
                });
                endpoints.MapGet("callback/{service}", async context => {
                    var service = context.Request.RouteValues["service"];
                    await HandleServiceCallback($"{service}", context);
                });
                endpoints.MapGet("widget/{widget}", async context => {
                    var widget = context.Request.RouteValues["widget"];
                    await HandleWidgetCallback($"{widget}", context);
                });
                endpoints.Map("widgetws/{widget}", async context => {
                    var widget = context.Request.RouteValues["widget"];
                    await HandleWidgetWSCallback($"{widget}", context);
                });
            });
        }
    }
}
