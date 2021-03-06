﻿using Smod2.EventHandlers;
using Smod2.Events;

namespace ZombiesBreakDoors {
    class MiscEventHandler : IEventHandlerWaitingForPlayers {
        private readonly ZBDPlugin plugin;

        public MiscEventHandler(ZBDPlugin plugin) => this.plugin = plugin;
        
        public void OnWaitingForPlayers(WaitingForPlayersEvent ev) {
            if (plugin.GetConfigBool("zbd_disable")) plugin.PluginManager.DisablePlugin(plugin);
        }
    }
}
