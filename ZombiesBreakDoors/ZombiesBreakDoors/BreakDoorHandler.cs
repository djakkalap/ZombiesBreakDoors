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


        // This constructor adds a reference to the plugin to this EventHandler.
        public BreakDoorHandler(ZBDPlugin plugin) {
            this.plugin = plugin;

            markedDoorsPos = new HashSet<int>();
        }

        // When a door is accessed by a zombie, we check if there are enough zombies near the door. If yes, we break it.
        public void OnDoorAccess(PlayerDoorAccessEvent ev) {
            int threshold = plugin.GetConfigInt("zbd_zombies_threshold");

            List<Player> zombies = plugin.Server.GetPlayers(Role.SCP_049_2);
            List<Player> nearbyZombies;

            if(ev.Player.TeamRole.Role.Equals(Role.SCP_049_2)) 
            {
                Smod2.API.Door door = ev.Door;

                // Only allow to destroy doors which normally can't be opened. Also check if there are even enough zombies in the round.
                if ((!ev.Allow && canBeBrokenDown(door)) && zombies.Count >= threshold)
                {
                    nearbyZombies = getZombiesNearby(door, zombies);

                    if (nearbyZombies.Count >= threshold) 
                    {
                        float delay = plugin.GetConfigFloat("zbd_delay");

                        // Marking the door for destruction
                        markedDoorsPos.Add(door.Position.GetHashCode());

                        // Display the countdown for each player
                        foreach(Player zombie in nearbyZombies) {
                            Timing.RunCoroutine(_displayCountdown(zombie, delay));
                        }
                        
                        Timing.RunCoroutine(_destroyDoorDelay(door, delay));
                    }
                }
            }
        }

        private IEnumerator<float> _displayCountdown(Player player, float seconds) {
            string message;

            for(int i = (int) seconds; i >= 0; i--) {
                message = "Breaking door in " + i + " seconds";
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

        // This method checks if the door is allowed to be destroyed.
        private bool canBeBrokenDown(Smod2.API.Door door) {
            bool breakIfOpen = plugin.GetConfigBool("zbd_breakopendoors");
            string[] disallowedDoors = plugin.GetConfigList("zbd_doors_disallow");

            if ((door.Open && breakIfOpen) || !door.Open) {
                if (!disallowedDoors.Contains(door.Name) && !markedDoorsPos.Contains(door.Position.GetHashCode())) {
                    return true;
                }
            }

            return false;
        }

        // Counts the amount of zombies within range of the door.
        private List<Player> getZombiesNearby(Smod2.API.Door door, List<Player> zombies) {
            List<Player> nearbyZombies = new List<Player>();
            Vector doorPos = door.Position;
            
            foreach(Player zombie in zombies) 
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
