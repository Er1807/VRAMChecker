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

[assembly: MelonInfo(typeof(VRAMCheckerMod), "VRAM Checker", "1.0.3", "Eric Fandenfart")]
[assembly: MelonGame]
namespace VRAMChecker
{

    internal class VRAMCheckerMod : MelonMod
    {
        private static ReMenuButton button;

        public override void OnApplicationStart()
        {
            VRAMCheckerInternal.LoggerInst = LoggerInstance;
            LoggerInstance.Msg("Loading VRAMCheckerMod v1.0.0");
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
                    button.Text = $"VRAM\n{VRAMCheckerInternal.GetSizeForAvatar(player._vrcplayer.field_Internal_GameObject_0)}";
                }
        }

        public IEnumerator InitQuickMenu()
        {
            while (GameObject.Find("UserInterface")?.GetComponentInChildren<VRC.UI.Elements.QuickMenu>(true) == null) yield return null;

            button = new ReMenuButton("VRAM\n-", "Click to recalculate VRAM size of avatar", ButtonClick, QuickMenuEx.SelectedUserLocal.transform.Find("ScrollRect/Viewport/VerticalLayoutGroup/Buttons_UserActions"), ResourceManager.GetSprite("remod.ram"));
            
            new ReMenuButton("Log VRAM", $"Logs the VRAM size of all avatars", VRAMCheckerInternal.LogInstance, QuickMenuEx.Instance.transform.Find("Container/Window/QMParent/Menu_Settings/Panel_QM_ScrollRect/Viewport/VerticalLayoutGroup/Buttons_Comfort"), ResourceManager.GetSprite("remod.ram"));
            
            LoggerInstance.Msg("Added Buttons");
        }

        public void ButtonClick()
        {
            string userid = QuickMenuEx.SelectedUserLocal.field_Private_IUser_0.GetUserID();
            foreach (Player player in PlayerManager.field_Private_Static_PlayerManager_0.field_Private_List_1_Player_0)
                if (player.prop_APIUser_0.id == userid)
                    button.Text = $"VRAM\n{VRAMCheckerInternal.GetSizeForAvatar(player._vrcplayer.field_Internal_GameObject_0)}";
        }
    }
}
