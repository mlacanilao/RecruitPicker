using UnityEngine;

namespace RecruitPicker.Patches
{
    public static class FactionBranchPatch
    {
        public static void AddRecruitPrefix(FactionBranch __instance, ref Chara c)
        {
            if (EClass.core?.IsGameStarted == false ||
                __instance == null ||
                c == null)
            {
                return;
            }
            
            var recruitIds = RecruitPickerConfig.SelectedRecruits;

            if (recruitIds.Count > 0)
            {
                int randomIndex = EClass.rnd(a: recruitIds.Count);
                string selectedRecruitId = recruitIds[index: randomIndex];
                c = CharaGen.Create(id: selectedRecruitId, lv: __instance.ContentLV + Mathf.Min(a: EClass.player.stats.days, b: 10));
            }
            else
            {
                RecruitPicker.Log(payload: "No recruit IDs available in config; falling back to default recruit generation.");
            }
        }
    }
}