using MelonLoader;
using ReMod.Core;
using ReMod.Core.Managers;
using ReMod.Core.UI.QuickMenu;
using ReMod.Core.VRChat;
using System;
using System.Collections;
using UnityEngine;
using VRC;
using VRC.DataModel;

namespace VRAMChecker
{
    public class RemodVRAMAddonComponent : ModComponent
    {
        internal static MelonLogger.Instance LoggerInst = new MelonLogger.Instance("ReMod-VRAM", ConsoleColor.DarkBlue);
        private static ReMenuButton button;
        
        public override void OnSelectUser(IUser user, bool isRemote)
        {
            string userid = user.GetUserID();
            foreach (Player player in PlayerManager.field_Private_Static_PlayerManager_0.field_Private_List_1_Player_0)
                if (player.prop_APIUser_0.id == userid)
                {
                    button.Text = $"VRAM\n{VRAMCheckerInternal.GetSizeForAvatar(player._vrcplayer.field_Internal_GameObject_0)}";
                }
        }

        public override void OnUiManagerInit(UiManager uiManager)
        {
            VRAMCheckerInternal.LoggerInst = LoggerInst;
            LoggerInst.Msg("Loading VRAM Addon v1.0.3");
            MelonCoroutines.Start(InitQuickMenu());

        }

        public static IEnumerator InitQuickMenu()
        {
            while (GameObject.Find("UserInterface").GetComponentInChildren<VRC.UI.Elements.QuickMenu>(true) == null) yield return null;

            var localMenu = new ReCategoryPage(QuickMenuEx.SelectedUserLocal.transform);
            var category = localMenu.GetCategory("ReModv5");
            button = category.AddButton("VRAM\n-", "Click to recalculate VRAM size of avatar", ButtonClick, ResourceManager.GetSprite("remod.ram"));


            ReMenuPage avatarPage = new ReMenuPage(QuickMenuEx.Instance.field_Public_Transform_0.Find("Window/QMParent/Menu_Avatars"));
            avatarPage.AddButton("Log VRAM", $"Logs the VRAM size of all avatars", VRAMCheckerInternal.LogInstance, ResourceManager.GetSprite("remod.ram"));

            LoggerInst.Msg("Added Buttons");
        }

        public static void ButtonClick()
        {
            string userid = QuickMenuEx.SelectedUserLocal.field_Private_IUser_0.GetUserID();
            foreach (Player player in PlayerManager.field_Private_Static_PlayerManager_0.field_Private_List_1_Player_0)
                if (player.prop_APIUser_0.id == userid)
                {
                    button.Text = $"VRAM\n{VRAMCheckerInternal.GetSizeForAvatar(player._vrcplayer.field_Internal_GameObject_0)}";

                }
        }

    }
}
