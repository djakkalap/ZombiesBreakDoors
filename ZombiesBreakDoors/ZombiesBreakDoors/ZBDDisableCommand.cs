using Smod2.Commands;

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

            return new string[] {GetUsage() + " called."};
        }
    }
}
