using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using vtortola.WebSockets;
using System.Threading.Tasks;
using Autodesk.Revit.UI;
using Dynamo.Graph.Workspaces;

namespace Dynamo.Applications
{
    class PlaylistServer
    {
        private static WebSocketListener _server;
        private DynamoRevitApp _mainApp;        

        public PlaylistServer(DynamoRevitApp theApp)
        {
            _mainApp = theApp;
        }

        static void Log(String line)
        {
            Console.WriteLine(line);
        }

        public void Start()
        {
            CancellationTokenSource cancellation = new CancellationTokenSource();

            var endpoint = new IPEndPoint(IPAddress.Any, 8005);
            _server = new WebSocketListener(endpoint, new WebSocketListenerOptions() { SubProtocols = new[] { "text" } });
            var rfc6455 = new vtortola.WebSockets.Rfc6455.WebSocketFactoryRfc6455(_server);
            _server.Standards.RegisterStandard(rfc6455);
            _server.Start();

            Log("Mono Echo Server started at " + endpoint.ToString());

            var task = Task.Run(() => AcceptWebSocketClientsAsync(_server, cancellation.Token, _mainApp));

//             Console.ReadKey(true);
//             Log("Server stoping");
//             cancellation.Cancel();
//             task.Wait();
//             Console.ReadKey(true);
        }

        static async Task AcceptWebSocketClientsAsync(WebSocketListener server, CancellationToken token, DynamoRevitApp revitApp)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var ws = await server.AcceptWebSocketAsync(token);
                    Log("Connected " + ws ?? "Null");
                    if (ws != null)
                        Task.Run(() => HandleConnectionAsync(ws, token, revitApp));
                }
                catch (Exception aex)
                {
                    Log("Error Accepting clients: " + aex.GetBaseException().Message);
                }
            }
            Log("Server Stop accepting clients");
        }

        static async Task HandleConnectionAsync(WebSocket ws, CancellationToken cancellation, DynamoRevitApp revitApp)
        {
            try
            {
                while (ws.IsConnected && !cancellation.IsCancellationRequested)
                {
                    String msg = await ws.ReadStringAsync(cancellation).ConfigureAwait(false);
                    Log("Message: " + msg);
                    //ws.WriteString(msg);

                    DynamoRevit.revitDynamoModel.OpenFileFromPath(msg);
                    HomeWorkspaceModel modelToRun = DynamoRevit.revitDynamoModel.CurrentWorkspace as HomeWorkspaceModel;
                    if (modelToRun != null)
                        modelToRun.Run();
                    //DynamoRevitApp.AddIdleAction(DynamoRevitApp.postPlayListExecution);

                    //revitApp.executePlaylistScript();

                }
            }
            catch (Exception aex)
            {
                Log("Error Handling connection: " + aex.GetBaseException().Message);
                try { ws.Close(); }
                catch { }
            }
            finally
            {
                ws.Dispose();
            }
        }
    }
}


