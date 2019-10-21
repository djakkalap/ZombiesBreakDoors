﻿using Smod2;
using Smod2.Attributes;
using Smod2.Config;
using Smod2.EventHandlers;
using Smod2.Events;

namespace ZombiesBreakDoors {
    [PluginDetails(
        author = "Argon",
        name = "Zombies Break Doors",
        description = "This plugin allows zombies to gather near a door and break it.",
        id = "argon.zbdplugin",
        configPrefix = "zbd",
        version = "0.2",
        SmodMajor = 3,
        SmodMinor = 5,
        SmodRevision = 0
        )]

    public class ZBDPlugin : Plugin {
        // Display info on disable.
        public override void OnDisable() {
            this.Info(this.Details.name + " has been disabled.");
        }

        // Display info on enable.
        public override void OnEnable() {
            this.Info(this.Details.name + " has been enabled.");
        }

        // Register the parts of the plugin.
        public override void Register() {
            // Add new config settings
            this.AddConfig(new ConfigSetting("zbd_zombies_threshold", 3, true, "This number determines how many zombies need to be near a door for it to break."));
            this.AddConfig(new ConfigSetting("zbd_zombies_range", 2.5f, true, "This number determines how far the zombies need to be in range for them to be considered 'near the door'."));
            this.AddConfig(new ConfigSetting("zbd_breakopendoors", false, true, "This boolean determines whether or not to break open doors."));

            this.AddEventHandler(typeof(IEventHandlerDoorAccess), new BreakDoorHandler(this), Priority.High);
        }
    }
}
