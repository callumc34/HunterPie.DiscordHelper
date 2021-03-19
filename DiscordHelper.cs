using System;
using System.IO;
using System.Security.Cryptography;
using System.Web;
using HunterPie.Core;
using WebSocketSharp;
using Newtonsoft.Json;

using Debugger = HunterPie.Logger.Debugger;

//TODO(Callum): Constant ping pong heartbeats for heroku to stay alive

static class Constants
{
    public const string FALLBACK_URI = "wss://server-mhwdiscordhelper.herokuapp.com/";
}

namespace HunterPie.Plugins
{
    public class DiscordHelper : IPlugin
    {

        internal class ModConfig
        {
            public string uri;
            public string id;
        }

        public string Name { get; set; }
        public string Description { get; set; }
        public Game Context { get; set; }

        
        private WebSocket WsClient;

        private string configPath = Path.Combine(Environment.CurrentDirectory, "Modules\\DiscordHelper", "Config.json");
        private ModConfig config;

        public void Initialize(Game context)
        {
            Name = "DiscordHelper";
            Description = "Performs useful interactions with a discord bot.";

            Context = context;

            if (!File.Exists(configPath))
            {
                Debugger.Error("Config.json for DiscordHelper not found!");
                Debugger.Module("Creating Config.json.", Name);
                File.WriteAllText(configPath,"{\n\t\"uri\": \"" + Constants.FALLBACK_URI + "\",\n\t\"id\": \"" + CreateUniqueID() + "\"\n}");
            } 
            string configSerialized = File.ReadAllText(configPath);
            config = JsonConvert.DeserializeObject<ModConfig>(configSerialized);

            SetupWsClient(config.uri, config.id);

        }

        public string CreateUniqueID()
        {
            var crypt = new RNGCryptoServiceProvider();
            var buf = new byte[64];

            crypt.GetBytes(buf);

            return Convert.ToBase64String(buf);
        }


        public void Unload()
        {
            WsClient.Close();
            WsClient = null;
            Context = null;
        }

        private void handleMessage(object sender, MessageEventArgs e)
        {
            string[] info = e.Data.Split(';');
            //Wrong id assigned or error
            if (info[0] != config.id)
            {
                return;
            }
            switch (info[1]) {
                case "heartbeat":
                    WsClient.Send(string.Format("{0};heartbeat;", config.id));
                    break;
                case "request-sid":
                    WsClient.Send(string.Format("{0};sid;1;{1};", config.id, Context.Player.SessionID));
                    break;
                case "request-build":
                    WsClient.Send(string.Format("{0};build;1;{1};", config.id,
                        HttpUtility.UrlEncode(Honey.LinkStructureBuilder(Context.Player.GetPlayerGear()))));
                    break;
                default:
                    //Invalid command recieved
                    break;
            }
        }

        private int SetupWsClient(string uri, string id)
        {
            WsClient = new WebSocket(uri + "?uniqueid=" + HttpUtility.UrlEncode(id));

            WsClient.OnOpen += (sender, e) =>
            {
                //Send confirmation message to HunterPie
                Debugger.Module("Connected to discord server.", Name);
            };

            WsClient.OnClose += (sender, e) =>
            {
                Debugger.Module("Close code: " + e.Code + " Close reason: " + e.Reason, Name);
                Debugger.Module("Disconnected from discord server. Restart plugin to reconnect.", Name);
            };

            WsClient.OnMessage += (sender, e) =>
            {
                handleMessage(sender, e);
            };

            WsClient.Connect();

            return 1;
        }
    }
}
