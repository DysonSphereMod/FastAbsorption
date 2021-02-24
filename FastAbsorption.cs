using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FastAbsorption
{
    [BepInPlugin("com.brokenmass.plugin.DSP.FastAbsorption", "FastAbsorption", "1.0.0")]
    public class FastAbsorption : BaseUnityPlugin
    {
        Harmony harmony;

        static private readonly int SPEED_MULTIPLIER = 100;

        void Start()
        {
            harmony = new Harmony("com.brokenmass.plugin.DSP.FastAbsorption");
            try
            {
                harmony.PatchAll(typeof(DysonSphereLayer_GameTick_Patch));
                harmony.PatchAll(typeof(DysonSwarm_AbsorbSail_Patch));

                Debug.Log($"[FastAbsorption Mod] Sail absorption speed multiplier : {SPEED_MULTIPLIER}x");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        internal void OnDestroy()
        {
            // For ScriptEngine hot-reloading
            harmony.UnpatchSelf();
        }

        [HarmonyPatch(typeof(DysonSphereLayer), "GameTick")]
        public static class DysonSphereLayer_GameTick_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var code = new List<CodeInstruction>(instructions);

                for (int i = 0; i < code.Count; i++)
                {
                    if (code[i].LoadsConstant(120L))
                    {
                        code[i].operand = (int)(120 / SPEED_MULTIPLIER);
                    }
                }

                return code.AsEnumerable();
            }
        }

        [HarmonyPatch(typeof(DysonSwarm), "AbsorbSail")]
        public static class DysonSwarm_AbsorbSail_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var code = new List<CodeInstruction>(instructions);

                for (int i = 0; i < code.Count; i++)
                {
                    if (code[i].LoadsConstant(14400L))
                    {
                        code[i].operand = (int)(14400L / SPEED_MULTIPLIER);
                        break;
                    }
                }

                return code.AsEnumerable();
            }
        }
    }
}