using Aki.Reflection.Patching;
using Aki.Reflection.Utils;
using System.Linq;
using System.Reflection;
using EFT;
using Comfort.Common;

namespace FloatingHealthbars
{
    class ExecuteOnRaidStartPatch : ModulePatch
    {
        // 0.13 method.
        private static Player LocalPlayer() => GamePlayerOwner.MyPlayer;

        // 12.12 method.
        private static Player LocalPlayerSingleton() => Singleton<GameWorld>.Instance.RegisteredPlayers.Find(p => p.IsYourPlayer);

        protected override MethodBase GetTargetMethod()
        {
            var desiredType = PatchConstants.EftTypes.Single(x => x.Name == "GameWorld");
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;

            var desiredMethod = desiredType.GetMethod("OnGameStarted", flags);

            Logger.LogDebug($"{this.GetType().Name} Type: {desiredType?.Name}");
            Logger.LogDebug($"{this.GetType().Name} Method: {desiredMethod?.Name}");

            return desiredMethod;
        }

        [PatchPrefix]
        private static bool PatchPrefix()
        {
            var player = LocalPlayerSingleton();

            // Start Mapcat in-raid execution.
            FHPlugin.Execute(player);

            return true;
        }
    }
}
