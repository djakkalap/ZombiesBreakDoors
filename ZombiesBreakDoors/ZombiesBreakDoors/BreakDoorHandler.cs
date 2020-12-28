using System;
using System.Linq;
using System.Collections.Generic;

using Smod2.API;
using Smod2.EventHandlers;
using Smod2.Events;
using MEC;

namespace ZombiesBreakDoors {
    class BreakDoorHandler : IEventHandlerDoorAccess {
        private readonly ZBDPlugin plugin;

        // This hash set contains the doors that are already marked for destruction and don't need to be checked anymore.
        // USING THE HASH OF DOOR POSITIONS BECAUSE THAT WAS THE ONLY PROPERTY OF THE DOORS THAT WAS CONSTANT.
        private HashSet<int> markedDoorsPos;

        private readonly string[] GATE_NAMES = { "GATE_A", "GATE_B", "914", "SURFACE_GATE", "372", "173", "079_FIRST", "079_SECOND" };

        // This set contains the players who are currently receiving a pbc from this plugin, this is used to prevent broadcasts from stacking.
        // It uses their SteamIds for this.
        private HashSet<string> gettingZombiesNeededBC;
        private HashSet<string> gettingCannotOpenBC;


        // This constructor adds a reference to the plugin to this EventHandler.
        public BreakDoorHandler(ZBDPlugin plugin) {
            this.plugin = plugin;

            markedDoorsPos = new HashSet<int>();
            gettingZombiesNeededBC = new HashSet<string>();
            gettingCannotOpenBC = new HashSet<string>();
        }

        // When a door is accessed by a zombie, we check if there are enough zombies near the door. If yes, we break it.
        public void OnDoorAccess(PlayerDoorAccessEvent ev) {
            int threshold = plugin.GetConfigInt("zbd_zombies_threshold");

            List<Player> zombies = plugin.Server.GetPlayers(Role.SCP_049_2);
            List<Player> nearbyZombies;
            int nearbyZombiesCount;

            if (ev.Player.TeamRole.Role == Smod2.API.RoleType.SCP_049_2) 
            {
                Smod2.API.Door door = ev.Door;

                // Don't check gates, because you can't open those anyways.
                if (GATE_NAMES.Contains(door.Name))
                {
                    return;
                }

                // Only allow to destroy doors which normally can't be opened.
                if (isDisallowed(door) && plugin.GetConfigBool("zbd_broadcast_cannotopen"))
                {
                    if (!gettingCannotOpenBC.Contains(ev.Player.SteamId))
                    {
                        Timing.RunCoroutine(_displayCantBreak(ev.Player));
                    }
                } else if (!ev.Allow && !isMarked(door) && canBeBrokenDown(door))
                {
                    nearbyZombies = getZombiesNearby(door, zombies);
                    nearbyZombiesCount = nearbyZombies.Count;

                    if (nearbyZombiesCount >= threshold) 
                    {
                        float delay = plugin.GetConfigFloat("zbd_delay");

                        // Marking the door for destruction
                        markedDoorsPos.Add(door.Position.GetHashCode());

                        if (plugin.GetConfigBool("zbd_broadcast_countdown")) 
                        {
                            // Display the countdown for each player
                            foreach (Player zombie in nearbyZombies) 
                            {
                                Timing.RunCoroutine(_displayCountdown(zombie, delay));
                            }
                        }
                        
                        Timing.RunCoroutine(_destroyDoorDelay(door, delay));
                    } else if (plugin.GetConfigBool("zbd_broadcast_zombiesneeded")) 
                    {
                        int amountNeeded = threshold - nearbyZombiesCount;

                        // Display zombies needed broadcast, don't do it for players already receiving this broadcast.
                        foreach (Player zombie in nearbyZombies) 
                        {
                            if (!gettingZombiesNeededBC.Contains(zombie.SteamId)) 
                            {
                                Timing.RunCoroutine(_displayZombiesNeeded(zombie, amountNeeded));
                            }
                        }
                    }
                }
            }
        }

        // Displays a broadcast (personal) using the PBC command. It shows a message that the door is being broken.
        private IEnumerator<float> _displayCountdown(Player player, float seconds) {
            string message;

            if (gettingZombiesNeededBC.Contains(player.SteamId))
            {
                player.PersonalClearBroadcasts();
            }

            for (int i = (int) seconds; i >= 0; i--) 
            {
                message = "<color=green>Breaking door in " + i + "</color>";

                plugin.CommandManager.CallCommand(new Smod2.Commands.ICommandSender(), "pbc", new string[] { player.Name, "1", message });

                yield return Timing.WaitForSeconds(1.0f);
            }
        }

        // Destroys a door with a set delay.
        private IEnumerator<float> _destroyDoorDelay(Smod2.API.Door door, float delay) {
            yield return Timing.WaitForSeconds(delay);

            Action<Smod2.API.Door> destroyDoor = (d) => d.Destroyed = true;

            destroyDoor(door);
        }

        // Displays how many zombies extra are needed near the door for it to break.
        private IEnumerator<float> _displayZombiesNeeded(Player player, int amountNeeded) {
            string message = "<color=cyan>You need " + amountNeeded + " more zombie(s) to break this door</color>";

            gettingZombiesNeededBC.Add(player.SteamId);
            plugin.CommandManager.CallCommand(new Smod2.Commands.ICommandSender(), "pbc", new string[] { player.Name, "1", message });

            yield return Timing.WaitForSeconds(1.0f);

            gettingZombiesNeededBC.Remove(player.SteamId);
        }

        // Displays that this door can't be broken.
        private IEnumerator<float> _displayCantBreak(Player player) {
            string message = "<color=red>This door cannot be broken</color>";

            if (gettingCannotOpenBC.Contains(player.SteamId))
            {
                player.PersonalClearBroadcasts();
            }

            gettingCannotOpenBC.Add(player.SteamId);
            plugin.CommandManager.CallCommand(new Smod2.Commands.ICommandSender(), "pbc", new string[] { player.Name, "1", message });

            yield return Timing.WaitForSeconds(1.0f);

            gettingCannotOpenBC.Remove(player.SteamId);
        }

        // This method checks if the door is allowed to be destroyed.
        private bool canBeBrokenDown(Smod2.API.Door door) {
            bool breakIfOpen = plugin.GetConfigBool("zbd_breakopendoors");

            if ((door.Open && breakIfOpen) || !door.Open) 
            {
                if (!isDisallowed(door))
                {
                    return true;
                }
            }

            return false;
        }

        // Checks if a door will be destroyed
        private bool isMarked(Smod2.API.Door door) {
            return markedDoorsPos.Contains(door.Position.GetHashCode());
        }

        // Checks if a door is in the disallowed list.
        private bool isDisallowed(Smod2.API.Door door) {
            string[] disallowedDoors = plugin.GetConfigList("zbd_doors_disallow");

            return disallowedDoors.Contains(door.Name);
        }

        // Counts the amount of zombies within range of the door.
        private List<Player> getZombiesNearby(Smod2.API.Door door, List<Player> zombies) {
            List<Player> nearbyZombies = new List<Player>();
            Vector doorPos = door.Position;
            
            foreach (Player zombie in zombies) 
            {
                Vector zombiePos = zombie.GetPosition();

                if(Vector.Distance(zombiePos, doorPos) <= plugin.GetConfigFloat("zbd_zombies_range")) 
                {
                    nearbyZombies.Add(zombie);
                }
            }

            return nearbyZombies;
        }
    }
}
