using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace FastAbsorption
{
    [BepInPlugin("com.brokenmass.plugin.DSP.FastAbsorption", "FastAbsorption", "1.2.0")]
    public class FastAbsorption : BaseUnityPlugin
    {
        Harmony harmony;

        public static ConfigEntry<int> frequencyMultiplier;
        public static ConfigEntry<int> travelSpeedMultiplier;

        // Cached field access to GameHistoryData.dysonNodeAbsorbInterval
        static FieldInfo absorbIntervalField;

        void Awake()
        {
            frequencyMultiplier = Config.Bind<int>("General", "frequencyMultiplier", 10, "How much more frequently should sail be requested by every DysonSphere node [Value must be between 1 (no effect - one sail every 2 seconds) and 120 (one sail every frame)]");
            travelSpeedMultiplier = Config.Bind<int>("General", "travelSpeedMultiplier", 1, "How much faster do sails take to travel to the requesting node. Values greater than 1 make the sail teleport to the proximity of the target node. [Value must be between 1 (no effect - 4 minutes travel time) and 120 (2 second travel time)]");

            frequencyMultiplier.Value = Math.Min(Math.Max(frequencyMultiplier.Value, 1), 120); // clamping value between 1 and 120
            travelSpeedMultiplier.Value = Math.Min(Math.Max(travelSpeedMultiplier.Value, 1), 120); // clamping value between 1 and 120

            // The game no longer hard-codes the absorb interval as a constant 120 in
            // DysonSphereLayer.GameTick. It is now the field GameHistoryData.dysonNodeAbsorbInterval
            // (gameData.history.dysonNodeAbsorbInterval), read and scaled at runtime.
            try
            {
                absorbIntervalField = AccessTools.Field(
                    AccessTools.TypeByName("GameHistoryData"), "dysonNodeAbsorbInterval");
            }
            catch (Exception e)
            {
                Logger.LogError("Failed to find GameHistoryData.dysonNodeAbsorbInterval: " + e.Message);
            }

            harmony = new Harmony("com.brokenmass.plugin.DSP.FastAbsorption");
            // Patch each target separately so a failure in one does not prevent the others.
            try
            {
                harmony.Patch(
                    AccessTools.Method(typeof(DysonSphereLayer), "GameTick"),
                    postfix: new HarmonyMethod(typeof(FastAbsorption), "DysonSphereLayer_GameTick_Postfix"));
            }
            catch (Exception e)
            {
                Logger.LogError("Failed to patch DysonSphereLayer.GameTick: " + e.Message);
            }
            try
            {
                harmony.Patch(
                    AccessTools.Method(typeof(DysonSwarm), "AbsorbSail"),
                    transpiler: new HarmonyMethod(typeof(FastAbsorption), "DysonSwarm_AbsorbSail_Transpiler"));
            }
            catch (Exception e)
            {
                Logger.LogError("Failed to patch DysonSwarm.AbsorbSail: " + e.Message);
            }

            Logger.LogInfo($"[FastAbsorption] frequencyMultiplier : {frequencyMultiplier.Value}x | travelSpeedMultiplier : {travelSpeedMultiplier.Value}x");
        }

        internal void OnDestroy()
        {
            // For ScriptEngine hot-reloading
            harmony.UnpatchSelf();
        }

        // ------------------------------------------------------------------
        // Patch 1: DysonSphereLayer.GameTick
        //
        // Current game code:
        //     int dysonNodeAbsorbInterval = gameData.history.dysonNodeAbsorbInterval;
        //     for (int i = (int)(gameTick % dysonNodeAbsorbInterval);
        //          i < nodeCursor; i += dysonNodeAbsorbInterval)
        //
        // The absorb interval is no longer a compile-time constant, so a
        // transpiler cannot find it. Instead we postfix the method and scale
        // the field on the game's history data to interval / frequencyMultiplier.
        // The postfix is skipped when the multiplier is 1 (no effect) or when
        // the field could not be resolved.
        // ------------------------------------------------------------------
        [HarmonyPatch(typeof(DysonSphereLayer), "GameTick")]
        [HarmonyPostfix]
        static void DysonSphereLayer_GameTick_Postfix(DysonSphereLayer __instance)
        {
            if (frequencyMultiplier == null || frequencyMultiplier.Value <= 1 || absorbIntervalField == null)
                return;

            try
            {
                var gameData = __instance.gameData;
                if (gameData == null) return;

                var history = gameData.history;
                if (history == null) return;

                int interval = (int)absorbIntervalField.GetValue(history);
                if (interval <= 0) return;

                int newInterval = Math.Max(1, interval / frequencyMultiplier.Value);
                if (newInterval != interval)
                    absorbIntervalField.SetValue(history, newInterval);
            }
            catch (Exception)
            {
                // swallow: the postfix must never break the game tick
            }
        }

        // ------------------------------------------------------------------
        // Patch 2: DysonSwarm.AbsorbSail
        //
        // The sail travel time is still a hard-coded constant 14400 (in
        // game-ticks, 4 minutes) in the current game:
        //     absorbOrder[num2].time = gameTick + 14400;
        //
        // Transpiler: replace the constant 14400 with 14400 / travelSpeedMultiplier.
        // The game loads the constant with ldc.i4 (32-bit), so the operand is
        // replaced as an int - Harmony refuses to change an ldc.i4 into an ldc.i8.
        // ------------------------------------------------------------------
        [HarmonyPatch(typeof(DysonSwarm), "AbsorbSail")]
        static IEnumerable<CodeInstruction> DysonSwarm_AbsorbSail_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = new List<CodeInstruction>(instructions);
            int multiplier = travelSpeedMultiplier != null ? travelSpeedMultiplier.Value : 1;
            if (multiplier <= 1)
                return code;

            for (int i = 0; i < code.Count; i++)
            {
                if (code[i].LoadsConstant(14400L))
                {
                    code[i].operand = (int)(14400L / multiplier);
                    break;
                }
            }

            return code;
        }
    }
}
