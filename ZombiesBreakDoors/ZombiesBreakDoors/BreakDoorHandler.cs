﻿using System;
using System.Linq;
using System.Collections.Generic;

using Smod2.API;
using Smod2.EventHandlers;
using Smod2.Events;
using MEC;

namespace ZombiesBreakDoors {
    class BreakDoorHandler : IEventHandlerDoorAccess {
        private readonly ZBDPlugin plugin;

        // This constructor adds a reference to the plugin to this EventHandler.
        public BreakDoorHandler(ZBDPlugin plugin) {
            this.plugin = plugin;
        }

        // When a door is accessed by a zombie, we check if there are enough zombies near the door. If yes, we break it.
        public void OnDoorAccess(PlayerDoorAccessEvent ev) {
            int threshold = plugin.GetConfigInt("zbd_zombies_threshold");

            List<Player> zombies = plugin.Server.GetPlayers(Role.SCP_049_2);

            if(ev.Player.TeamRole.Role.Equals(Role.SCP_049_2)) 
            {
                // Only allow to destroy doors which normally can't be opened.
                if(!ev.Allow && canBeDestroyed(ev.Door)) 
                {
                    if (zombies.Count >= threshold) 
                    {
                        if (getZombiesNearby(ev.Door, zombies) >= threshold) 
                        {
                            Timing.RunCoroutine(_destroyDoorDelay(ev.Door, plugin.GetConfigFloat("zbd_delay")));
                        }
                    }
                }            
            }
        }

        // Destroys a door with a set delay.
        private IEnumerator<float> _destroyDoorDelay(Smod2.API.Door door, float delay) {
            yield return Timing.WaitForSeconds(delay);

            Action<Smod2.API.Door> destroyDoor = (d) => d.Destroyed = true;

            destroyDoor(door);
        }

        // This method checks if the door is allowed to be destroyed.
        private bool canBeDestroyed(Smod2.API.Door door) {
            bool breakIfOpen = plugin.GetConfigBool("zbd_breakopendoors");
            string[] disallowedDoors = plugin.GetConfigList("zbd_doors_disallow");

            if ((door.Open && breakIfOpen) || !door.Open) {
                if(!disallowedDoors.Contains(door.Name)) {
                    return true;
                }
            }

            return false;
        }

        // Counts the amount of zombies within range of the door.
        private int getZombiesNearby(Smod2.API.Door door, List<Player> zombies) {
            Vector doorPos = door.Position;
            int zombieCount = 0;
            
            foreach(Player zombie in zombies) 
            {
                Vector zombiePos = zombie.GetPosition();

                if(Vector.Distance(zombiePos, doorPos) <= plugin.GetConfigFloat("zbd_zombies_range")) 
                {
                    zombieCount++;
                }
            }

            return zombieCount;
        }
    }
}
