using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;

namespace ConfirmWarpTotem
{
    internal class ObjectPatches
    {
        // initialized by ModEntry.cs
        public static IMonitor ModMonitor; // allow patches to call ModMonitor.Log()
        public static string ConfirmWarpTotemFormat;

        public static bool fromConfirmation = false;
        public static StardewValley.Object warpTotemBeingConfirmed = null;
        public static GameLocation locationBeingConfirmed = null;

        public static bool Object_performUseAction_Prefix(GameLocation location, StardewValley.Object __instance)
        {
            // If we should ask for confirmation, then do so and return false (skips base game function)
            // Otherwise, return true (runs base game function normally)
            try
            {
                // Would the base game reach the code for "warp totem is being used"?

                if (!Game1.player.canMove)
                {
                    ModMonitor.Log($"[Confirm Warp Totem] Ignoring object use (player can't move)", LogLevel.Trace);
                    return true;
                }
                if (__instance.isTemporarilyInvisible)
                {
                    ModMonitor.Log($"[Confirm Warp Totem] Ignoring object use (object is temporarily invisible)", LogLevel.Trace);
                    return true;
                }
                if (__instance.name == null)
                {
                    ModMonitor.Log($"[Confirm Warp Totem] Ignoring object use (object has no name)", LogLevel.Trace);
                    return true;
                }
                if (!__instance.name.Contains("Totem"))
                {
                    ModMonitor.Log($"[Confirm Warp Totem] Ignoring object use (object isn't a totem)", LogLevel.Trace);
                    return true;
                }

                // normal_gameplay
                if (Game1.eventUp)
                {
                    ModMonitor.Log($"[Confirm Warp Totem] Ignoring object use (event up)", LogLevel.Trace);
                    return true;
                }
                if (Game1.isFestival())
                {
                    ModMonitor.Log($"[Confirm Warp Totem] Ignoring object use (festival)", LogLevel.Trace);
                    return true;
                }
                if (Game1.fadeToBlack)
                {
                    ModMonitor.Log($"[Confirm Warp Totem] Ignoring object use (fade to black)", LogLevel.Trace);
                    return true;
                }
                if (Game1.player.swimming.Value)
                {
                    ModMonitor.Log($"[Confirm Warp Totem] Ignoring object use (swimming)", LogLevel.Trace);
                    return true;
                }
                if (Game1.player.bathingClothes.Value)
                {
                    ModMonitor.Log($"[Confirm Warp Totem] Ignoring object use (bathing clothes)", LogLevel.Trace);
                    return true;
                }
                if (Game1.player.onBridge.Value)
                {
                    ModMonitor.Log($"[Confirm Warp Totem] Ignoring object use (on bridge)", LogLevel.Trace);
                    return true;
                }

                // Is the object a warp totem?
                // Future improvement: allow whitelisting/blacklisting items in config file

                var isWarpTotem = false;
                switch (__instance.ParentSheetIndex)
                {
                    case 261: // desert
                    case 688: // farm
                    case 689: // mountains
                    case 690: // beach
                    case 886: // island
                        ModMonitor.Log($"[Confirm Warp Totem] Standard warp totem {__instance.name}", LogLevel.Debug);
                        isWarpTotem = true;
                        break;

                    case 681: // rain
                        ModMonitor.Log($"[Confirm Warp Totem] Ignoring object use (rain totem)", LogLevel.Trace);
                        isWarpTotem = false;
                        break;

                    default:
                        ModMonitor.Log($"[Confirm Warp Totem] Unknown object type {__instance.name} (ID {__instance.ParentSheetIndex})", LogLevel.Debug);
                        isWarpTotem = __instance.name.Contains("Warp");
                        break;
                }

                if (!isWarpTotem)
                {
                    return true;
                }

                // If farmer just confirmed, then reset mod state and let normal action occur
                if (fromConfirmation)
                {
                    ModMonitor.Log($"[Confirm Warp Totem] Got confirmation", LogLevel.Debug);
                    warpTotemBeingConfirmed = null;
                    locationBeingConfirmed = null;
                    fromConfirmation = false;
                    return true;
                }

                // If someone else's confirmation is pending, then give up and let normal action occur
                // Future improvement: track pending confirmations on a per-farmer basis
                if (warpTotemBeingConfirmed != null || locationBeingConfirmed != null)
                {
                    ModMonitor.Log($"[Confirm Warp Totem] Mod can't handle overlapping confirmations in multiplayer", LogLevel.Debug);
                    return true;
                }

                // Future improvement: allow whitelisting/blacklisting locations where game should ask for confirmation in config file

                // Record mod state
                warpTotemBeingConfirmed = __instance;
                locationBeingConfirmed = location;

                // Generate confirmation
                ModMonitor.Log($"[Confirm Warp Totem] Asking for confirmation", LogLevel.Debug);
                // this creates a mid-screen OK/cancel dialog, e.g. "are you sure you want to leave the festival?"
                // Future improvement: option to use createQuestionDialogue() instead (bottom-of-screen yes/no dialog)
                Game1.activeClickableMenu = new ConfirmationDialog(string.Format(ConfirmWarpTotemFormat, __instance.name), totemWarpConfirmed, totemWarpCanceled);

                // Block normal action
                return false;
            }
            catch (Exception ex)
            {
                ModMonitor.Log($"[Confirm Warp Totem] Object_performUseAction_Prefix: {ex.Message} - {ex.StackTrace}", LogLevel.Error);
                return true;
            }
        }

        private static void totemWarpConfirmed(Farmer who)
        {
            // Reset UI state
            Game1.exitActiveMenu();

            // If we lost track of mod state, then give up and exit

            if (warpTotemBeingConfirmed == null)
            {
                ModMonitor.Log($"[Confirm Warp Totem] Ignoring confirmation (lost track of warp totem)", LogLevel.Error);
                return;
            }
            if (locationBeingConfirmed == null)
            {
                ModMonitor.Log($"[Confirm Warp Totem] Ignoring confirmation (lost track of location)", LogLevel.Error);
                return;
            }

            // Indicate that it was confirmed
            fromConfirmation = true;

            // Trigger normal action
            ModMonitor.Log($"[Confirm Warp Totem] Processing confirmation", LogLevel.Trace);
            warpTotemBeingConfirmed.performUseAction(locationBeingConfirmed);

            // Use up the totem
            who.reduceActiveItemByOne();
        }

        private static void totemWarpCanceled(Farmer who)
        {
            // Reset UI state
            Game1.exitActiveMenu();

            // Reset mod state
            ModMonitor.Log($"[Confirm Warp Totem] Warp totem was canceled", LogLevel.Debug);
            fromConfirmation = false;
            warpTotemBeingConfirmed = null;
            locationBeingConfirmed = null;
        }

    }
}
