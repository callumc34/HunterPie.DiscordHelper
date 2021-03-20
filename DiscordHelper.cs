using System;
using System.Linq;
using System.Collections.Generic;
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

        public class DamageInformation
        {
            public float DamageValue { get; set; }
            public string DamageMessage { get; set; }
        }

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

        public string getDps(int[] range)
        {
            //Copied from DamageChat plugin - https://github.com/ricochhet/HunterPie.DamageChat/blob/master/plugin/plugin.cs
            List<Member> members = Context.Player.PlayerParty.Members;
            List<DamageInformation> damageInformation = new List<DamageInformation>();
            foreach (Member member in members)
            {
                if (member.Name != "" && member.Name != null)
                {
                    string DamageString = $"{member.Name}: {member.Damage} ({(Math.Floor(member.DamagePercentage * 100) / 100) * 100}%) damage";
                    damageInformation.Add(new DamageInformation { DamageValue = member.Damage, DamageMessage = DamageString });
                }
            }

            List<DamageInformation> ordered = damageInformation.OrderByDescending(dmg => dmg.DamageValue).ToList();
            List<DamageInformation> joinList;
            if (range.Length < 2)
            {
                joinList = new List<DamageInformation>();
                joinList.Add(ordered[range.First() - 1]);
            }
            else
            {
                if ((range.Last() - range.First()) > ordered.Count - range.First())
                {
                    joinList = ordered;
                }
                else
                {
                    joinList = ordered.GetRange(range.First() - 1, range.Last() - range.First() + 1);
                }
                
            }

            return string.Join(";", joinList.Select(obj => obj.DamageMessage));
        }

        private void handleMessage(object sender, MessageEventArgs e)
        {
            //info[0] = uniqueid
            //info[1] = command
            //info[2...] = args
            string[] info = e.Data.Split(';');
            info = info.Take(info.Length - 1).ToArray();
            //Wrong id assigned or error
            if (info[0] != config.id)
            {
                return;
            }
            string[] args = new string[info.Length - 2];
            for (var i = 2; i < info.Length; i++)
            {
                args[i - 2] = info[i];
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
                case "request-dps":
                    int i = 0;
                    int[] range = (from s in args where int.TryParse(s, out i) select i).ToArray();
                    if (range.Length == args.Length && range.Last() <= 4 && range.First() > 0) 
                    {
                        WsClient.Send(string.Format("{0};dps;{1};{2};", config.id, range.Length, getDps(range)));
                    }
                    else
                    {
                        WsClient.Send(string.Format("{0};error;1;invalid-arguments;", config.id));
                    }                    
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
