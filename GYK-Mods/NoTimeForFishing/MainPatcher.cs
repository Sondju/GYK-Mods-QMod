using System.Collections.Generic;
using System.Linq;
using Harmony;
using System.Reflection;
using System.Reflection.Emit;

namespace NoTimeForFishing
{
    public class MainPatcher
    {
        public static void Patch()
        {
            var val = HarmonyInstance.Create($"p1xel8ted.graveyardkeeper.NoTimeForFishing");
            val.PatchAll(Assembly.GetExecutingAssembly());
        }

        [HarmonyPatch(typeof(FishingGUI), "Update")]
        public static class PatchOutMiniGame
        {
            //skip the mini-game
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                //make the "pulling" and "awaiting pull" actions skip to looting
                    var codes = new List<CodeInstruction>(instructions);
                    codes[20] = codes[26];
                    codes[23] = codes[26];
                    return codes.AsEnumerable();
            }
        }


        [HarmonyPatch(typeof(FishingGUI), "UpdateTakingOut")]
        public static class PatchOutAnimationRequirement
        {
            //remove the check for the animation to finish
            //nop index 0 - 4
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);
                codes[0].opcode = OpCodes.Nop; //removes the check for fishing animation which never plays because it gets skipped
                codes[1].opcode = OpCodes.Nop;
                codes[2].opcode = OpCodes.Nop;
                codes[3].opcode = OpCodes.Nop;
                codes[14].opcode = OpCodes.Brtrue_S; //makes the games "successful fish" check always true
                return codes.AsEnumerable();
            }
        }
    }
}