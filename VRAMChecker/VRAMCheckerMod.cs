using MelonLoader;
using ReMod.Core.Managers;
using ReMod.Core.UI.QuickMenu;
using ReMod.Core.VRChat;
using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using UnhollowerRuntimeLib.XrefScans;
using UnityEngine;
using VRAMChecker;
using VRC;
using VRC.DataModel;
using VRC.UI.Elements.Menus;

[assembly: MelonInfo(typeof(VRAMCheckerMod), "VRAM Checker", VRAMCheckerInternal.Version, "Eric Fandenfart")]
[assembly: MelonGame]
namespace VRAMChecker
{

    internal class VRAMCheckerMod : MelonMod
    {
        private static ReMenuButton buttonSize, buttonSizeActive;

        public override void OnApplicationStart()
        {
            VRAMCheckerInternal.LoggerInst = LoggerInstance;
            LoggerInstance.Msg($"Loading VRAMCheckerMod v{VRAMCheckerInternal.Version}");
            MelonCoroutines.Start(InitQuickMenu());

            foreach (var method in typeof(SelectedUserMenuQM).GetMethods())
            {
                if (!method.Name.StartsWith("Method_Private_Void_IUser_PDM_"))
                    continue;

                if (XrefScanner.XrefScan(method).Count() < 3)
                    continue;

                HarmonyInstance.Patch(method, postfix: typeof(VRAMCheckerMod).GetMethod(nameof(OnSelectUser), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public).ToNewHarmonyMethod());
            }
            HarmonyInstance.Patch(typeof(VRCPlayer).GetMethod(nameof(VRCPlayer.Awake)), postfix: typeof(VRAMCheckerMod).GetMethod(nameof(VRCPlayerAwakePatch), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public).ToNewHarmonyMethod());
        }

        public static void OnSelectUser(IUser __0)
        {
            string userid = __0.GetUserID();
            foreach (Player player in PlayerManager.field_Private_Static_PlayerManager_0.field_Private_List_1_Player_0)
                if (player.prop_APIUser_0.id == userid)
                {
                    var result = VRAMCheckerInternal.GetSizeForPlayer(player);
                    buttonSize.Text = $"VRAM\n{result.VRAMString}";
                    buttonSizeActive.Text = $"VRAM (A)\n{result.VRAMActiveOnlyString}";
                }
        }

        public IEnumerator InitQuickMenu()
        {
            while (GameObject.Find("UserInterface")?.GetComponentInChildren<VRC.UI.Elements.QuickMenu>(true) == null) yield return null;

            buttonSize = new ReMenuButton("VRAM\n-", "Click to recalculate VRAM size of avatar", ButtonClick, QuickMenuEx.SelectedUserLocal.transform.Find("ScrollRect/Viewport/VerticalLayoutGroup/Buttons_UserActions"), ResourceManager.GetSprite("remod.ram"));
            buttonSizeActive = new ReMenuButton("VRAM (A)\n-", "Click to recalculate VRAM size of avatar", ButtonClick, QuickMenuEx.SelectedUserLocal.transform.Find("ScrollRect/Viewport/VerticalLayoutGroup/Buttons_UserActions"), ResourceManager.GetSprite("remod.ram"));

            new ReMenuButton("Log VRAM", $"Logs the VRAM size of all avatars", VRAMCheckerInternal.LogInstance, QuickMenuEx.Instance.transform.Find("Container/Window/QMParent/Menu_Settings/Panel_QM_ScrollRect/Viewport/VerticalLayoutGroup/Buttons_Comfort"), ResourceManager.GetSprite("remod.ram"));
            new ReMenuButton("Log VRAM", $"Logs the VRAM size of this World", VRAMCheckerInternal.LogWorld, QuickMenuEx.Instance.transform.Find("Container/Window/QMParent/Menu_Here/ScrollRect/Viewport/VerticalLayoutGroup/Buttons_WorldActions"), ResourceManager.GetSprite("remod.ram"));

            LoggerInstance.Msg("Added Buttons");
        }

        public void ButtonClick()
        {
            string userid = QuickMenuEx.SelectedUserLocal.field_Private_IUser_0.GetUserID();
            foreach (Player player in PlayerManager.field_Private_Static_PlayerManager_0.field_Private_List_1_Player_0)
                if (player.prop_APIUser_0.id == userid)
                {
                    var result = VRAMCheckerInternal.GetSizeForPlayer(player);
                    buttonSize.Text = $"VRAM\n{result.VRAMString}";
                    buttonSizeActive.Text = $"VRAM (A)\n{result.VRAMActiveOnlyString}";
                }
        }

        //https://github.com/RequiDev/ReModCE/blob/f61ee21238d2b1eb0830484495df0196d41b425a/ReModCE/ReModCE.cs#L384
        public static void VRCPlayerAwakePatch(VRCPlayer __instance)
        {
            if (__instance == null) return;

            __instance.Method_Public_add_Void_OnAvatarIsReady_0(new Action(() =>
            {
                new Task(() => VRAMCheckerInternal.GetTextureSizeAssetBundle(__instance.prop_Player_0)).Start();
            }));
        }
    }
}
