using System;
using System.Collections.Generic;
using HarmonyLib;
using RecruitPicker.Patches;

namespace RecruitPicker
{
    public class Patcher
    {
        [HarmonyPrefix]
        [HarmonyPatch(declaringType: typeof(FactionBranch), methodName: nameof(FactionBranch.AddRecruit))]
        public static void FactionBranchAddRecruit(FactionBranch __instance, ref Chara c)
        {
            FactionBranchPatch.AddRecruitPrefix(__instance: __instance, c: ref c);
        }
    }
}