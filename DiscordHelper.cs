using HunterPie.Core;

namespace HunterPie.Plugins
{
    public class DiscordHelper : IPlugin
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Game Context { get; set; }

        public void Initialize(Game context)
        {
            Name = "DiscordHelper";
            Description = "Performs useful interactions with a discord bot.";

            Context = context;
        }

        public void Unload()
        {

        }

    }
}
