using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using System.Linq;

namespace ConfirmWarpTotem
{
    public class ModEntry : Mod
    {
        /*********
        ** Properties
        *********/
        /// <summary>The mod configuration from the player.</summary>
        private ModConfig Config;

        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            this.Config = this.Helper.ReadConfig<ModConfig>();

            ObjectPatches.ModMonitor = this.Monitor;
            ObjectPatches.ConfirmWarpTotemFormat = Config.ConfirmWarpTotemFormat;

            var harmony = new Harmony(this.ModManifest.UniqueID);
            // detect when warp totem is used
            // can't patch totemWarp() because it's private, so must patch function that calls totemWarp()
            harmony.Patch(
               original: AccessTools.Method(typeof(StardewValley.Object), nameof(StardewValley.Object.performUseAction)),
               prefix: new HarmonyMethod(typeof(ObjectPatches), nameof(ObjectPatches.Object_performUseAction_Prefix))
            );
        }
    }
}
