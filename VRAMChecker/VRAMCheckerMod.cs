using MelonLoader;
using ReMod.Core.Managers;
using ReMod.Core.UI.QuickMenu;
using ReMod.Core.VRChat;
using System.Collections;
using System.Linq;
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
        private static VRAMCheckerInternal VRAMChecker;

        public override void OnApplicationStart()
        {
            VRAMChecker = new VRAMCheckerInternal(LoggerInstance);
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
        }

        public static void OnSelectUser(IUser __0)
        {
            string userid = __0.GetUserID();
            foreach (Player player in PlayerManager.field_Private_Static_PlayerManager_0.field_Private_List_1_Player_0)
                if (player.prop_APIUser_0.id == userid)
                {
                    var sizes = VRAMChecker.GetSizeForGameObject(player._vrcplayer.field_Internal_GameObject_0);
                    buttonSize.Text = $"VRAM\n{sizes.size}";
                    buttonSizeActive.Text = $"VRAM (A)\n{sizes.sizeOnlyActive}";
                }
        }

        public IEnumerator InitQuickMenu()
        {
            while (GameObject.Find("UserInterface")?.GetComponentInChildren<VRC.UI.Elements.QuickMenu>(true) == null) yield return null;

            buttonSize = new ReMenuButton("VRAM\n-", "Click to recalculate VRAM size of avatar", ButtonClick, QuickMenuEx.SelectedUserLocal.transform.Find("ScrollRect/Viewport/VerticalLayoutGroup/Buttons_UserActions"), ResourceManager.GetSprite("remod.ram"));
            buttonSizeActive = new ReMenuButton("VRAM (A)\n-", "Click to recalculate VRAM size of avatar", ButtonClick, QuickMenuEx.SelectedUserLocal.transform.Find("ScrollRect/Viewport/VerticalLayoutGroup/Buttons_UserActions"), ResourceManager.GetSprite("remod.ram"));

            new ReMenuButton("Log VRAM", $"Logs the VRAM size of all avatars", VRAMChecker.LogInstance, QuickMenuEx.Instance.transform.Find("Container/Window/QMParent/Menu_Settings/Panel_QM_ScrollRect/Viewport/VerticalLayoutGroup/Buttons_Comfort"), ResourceManager.GetSprite("remod.ram"));
            
            LoggerInstance.Msg("Added Buttons");
        }

        public void ButtonClick()
        {
            string userid = QuickMenuEx.SelectedUserLocal.field_Private_IUser_0.GetUserID();
            foreach (Player player in PlayerManager.field_Private_Static_PlayerManager_0.field_Private_List_1_Player_0)
                if (player.prop_APIUser_0.id == userid)
                {
                    var sizes = VRAMChecker.GetSizeForGameObject(player._vrcplayer.field_Internal_GameObject_0);
                    buttonSize.Text = $"VRAM\n{sizes.size}";
                    buttonSizeActive.Text = $"VRAM (A)\n{sizes.sizeOnlyActive}";
                }
        }
    }
}
