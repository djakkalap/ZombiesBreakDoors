using Smod2;
using Smod2.Commands;
using Smod2.Attributes;
using Smod2.Config;
using Smod2.EventHandlers;

namespace ZombiesBreakDoors {
    class ZBDDisableCommand : ICommandHandler {
        private readonly ZBDPlugin plugin;

        public ZBDDisableCommand(ZBDPlugin plugin) => this.plugin = plugin;

        public string GetCommandDescription() {
            return "Disables the ZombiesBreakDoors plugin.";
        }

        public string GetUsage() {
            return "ZBDDISABLE";
        }

        public string[] OnCall(ICommandSender sender, string[] args) {
            plugin.Info("Disabling " + plugin.Details.name + "...");
            plugin.PluginManager.DisablePlugin(plugin);

            return new string[] {plugin.Details.name + " disabled."};
        }
    }
}
