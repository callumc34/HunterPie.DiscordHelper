using System;
using System.IO;
using System.Security.Cryptography;
using HunterPie.Core;
using WebSocketSharp;
using Newtonsoft.Json;

using Debugger = HunterPie.Logger.Debugger;

static class Constants
{
    public const string FALLBACK_URI = "";
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
                File.WriteAllText(configPath,"{\n\t\"uri\": \"" + Constants.FALLBACK_URI + "\n\t\"id\": \"" + CreateUniqueID() + "\"\n}");
            } else
            {
                string configSerialized = File.ReadAllText(configPath);
                config = JsonConvert.DeserializeObject<ModConfig>(configSerialized);
            }

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
            WsClient.Close(CloseStatusCode.Away);
        }

        private int SetupWsClient(string uri, string id)
        {
            WsClient = new WebSocket(uri  +"/?" + id);

            WsClient.OnOpen += (sender, e) =>
            {
                //Send confirmation message to HunterPie
                Debugger.Module("Connected to discord server.", Name);
            };

            WsClient.OnClose += (sender, e) =>
            {
                Debugger.Module("Disconnected from discord server.", Name);
            };

            return 1;
        }
    }
}
