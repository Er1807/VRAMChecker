﻿using MelonLoader;
using ReMod.Core.UI.QuickMenu;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using VRC;

namespace VRAMChecker
{
    public class VRAMCheckerInternal
    {
        public const string Version = "1.0.4";
            
        private static Dictionary<TextureFormat, int> BPP = new Dictionary<TextureFormat, int>()
        {
            { TextureFormat.Alpha8, 8},
            { TextureFormat.ARGB4444, 16},
            { TextureFormat.RGB24, 8*3},
            { TextureFormat.RGBA32, 8*4},
            { TextureFormat.ARGB32, 8*4},
            { TextureFormat.RGB565, 16},
            { TextureFormat.R16, 16},
            { TextureFormat.DXT1, 4},
            { (TextureFormat)11, 8}, //not working
            { TextureFormat.DXT5, 8},
            { TextureFormat.RGBA4444, 4*4},
            { TextureFormat.BGRA32, 8*4},
            { TextureFormat.RHalf, 16},
            { TextureFormat.RGHalf, 16*2},
            { TextureFormat.RGBAHalf, 16*4},
            { TextureFormat.RFloat, 32},
            { TextureFormat.RGFloat, 32*2},
            { TextureFormat.RGBAFloat, 32*4},
            //{ TextureFormat.YUY2, x},	A format that uses the YUV color space and is often used for video encoding or playback.
            { TextureFormat.RGB9e5Float, 9*5+3},
            { TextureFormat.BC4, 4},
            { TextureFormat.BC5, 8},
            { TextureFormat.BC6H, 8},
            { TextureFormat.BC7, 8}, //on non supporteed hardware 32 RGBA32 at runtime
            { TextureFormat.DXT1Crunched, 4},
            { TextureFormat.DXT5Crunched, 8},
            { TextureFormat.PVRTC_RGB2, 2},
            { TextureFormat.PVRTC_RGBA2, 2},
            { TextureFormat.PVRTC_RGB4, 4},
            { TextureFormat.PVRTC_RGBA4, 4},
            { TextureFormat.ETC_RGB4, 4},
            { TextureFormat.EAC_R, 4},
            { TextureFormat.EAC_R_SIGNED, 4},
            { TextureFormat.EAC_RG, 8},
            { TextureFormat.EAC_RG_SIGNED, 8},
            { TextureFormat.ETC2_RGB, 4},
            { TextureFormat.ETC2_RGBA1, 4},
            { TextureFormat.ETC2_RGBA8, 8},
            { TextureFormat.ASTC_4x4, 8},
            { TextureFormat.ASTC_5x5, 5}, //5.12
            { TextureFormat.ASTC_6x6, 4}, //3.55
            { TextureFormat.ASTC_8x8, 2},
            { TextureFormat.ASTC_10x10, 1}, //1.28
            { TextureFormat.ASTC_12x12, 1}, //0.8
            { TextureFormat.RG16, 8*2},
            { TextureFormat.R8, 8},
            { TextureFormat.ETC_RGB4Crunched, 4}, //guessed
            { TextureFormat.ETC2_RGBA8Crunched, 8}, //guessed
            { TextureFormat.ASTC_HDR_4x4, 8},
            { TextureFormat.ASTC_HDR_5x5, 5}, //5.12
            { TextureFormat.ASTC_HDR_6x6, 4}, //3.55
            { TextureFormat.ASTC_HDR_8x8, 2},
            { TextureFormat.ASTC_HDR_10x10, 1}, //1.28
            { TextureFormat.ASTC_HDR_12x12, 1}, //0.8
            { TextureFormat.RG32, 16*2},
            { TextureFormat.RGB48, 16*3},
            { TextureFormat.RGBA64, 16*4}
        };

        private MelonLogger.Instance LoggerInst;
        private List<IntPtr> done = new List<IntPtr>();
        long VRAMSize = 0;
        long VRAMSizeOnlyActive = 0;

        public VRAMCheckerInternal(MelonLogger.Instance loggerInst)
        {
            LoggerInst = loggerInst;
        }

        public static void LogInstance(MelonLogger.Instance instance)
        {
            instance.Msg("Vram Sizes of Avatars in Lobby");
            foreach (Player player in PlayerManager.field_Private_Static_PlayerManager_0.field_Private_List_1_Player_0)
            {
                var avatarGameobj = player._vrcplayer.field_Internal_GameObject_0;
                var sizes = new VRAMCheckerInternal(instance).GetSizeForGameObject(avatarGameobj);


                instance.Msg($"{ player.field_Private_APIUser_0.displayName}({ player.field_Private_APIUser_0.id}) : Total: {sizes.size} OnlyActive; {sizes.sizeOnlyActive}");

            }
        }

        public (string size, string sizeOnlyActive) GetSizeForGameObject(GameObject avatar)
        {
            foreach (var item in avatar.GetComponentsInChildren<MeshRenderer>(true))
            {
                GetSizeForRenderer(item);
            }
            foreach (var item in avatar.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                GetSizeForRenderer(item);
            }
            foreach (var item in avatar.GetComponentsInChildren<MeshFilter>(true))
            {
                GetSizeForMesh(item.mesh, item.gameObject.activeInHierarchy);
            }

            return (ToByteString(VRAMSize), ToByteString(VRAMSizeOnlyActive));
        }

        private void GetSizeForRenderer(SkinnedMeshRenderer renderer)
        {
            foreach (var mat in renderer.materials)
            {
                GetSizeForMaterial(mat, renderer.gameObject.activeInHierarchy);
            }
            GetSizeForMesh(renderer.sharedMesh, renderer.gameObject.activeInHierarchy);
        }

        private void GetSizeForRenderer(MeshRenderer renderer)
        {
            foreach (var mat in renderer.materials)
            {
                GetSizeForMaterial(mat, renderer.gameObject.activeInHierarchy);
            }
        }

        private void GetSizeForMesh(Mesh mesh, bool isActive)
        {
            if (mesh == null) return;
            var total = Profiler.GetRuntimeMemorySizeLong(mesh);
            //LoggerInst.Msg($"DEBUG: Mesh: {mesh.name} Profiler.GetRuntimeMemorySizeLong(mesh): {total}");
            VRAMSize += total;
            if(isActive)
                VRAMSizeOnlyActive += total;
        }

        private void GetSizeForMaterial(Material mat, bool isActive)
        {
            if (mat == null) return;
            var texIds = mat.GetTexturePropertyNames();
            foreach (var id in texIds)
            {
                try
                {
                    var test = mat.GetTexture(id);
                    if (test == null)
                        continue;
                    GetSizeForTexture(test.Cast<Texture2D>(), isActive);
                }
                catch (Exception)
                {
                }
            }
        }

        private void GetSizeForTexture(Texture2D tex, bool isActive)
        {
            if (tex == null) return;
            if (done.Contains(tex.Pointer)) return;
            done.Add(tex.Pointer);
            TextureFormat textureFormat = tex.format;

            if (!BPP.ContainsKey(tex.format))
            {
                LoggerInst.Warning("Does not have BPP for " + textureFormat);
                return;
            }
            
            double mipmaps = 1;
            for (int i = 0; i < tex.mipmapCount; i++) mipmaps += Math.Pow(0.25, i + 1);
            var bytesCount = (long)(BPP[textureFormat] * tex.width * tex.height * mipmaps / 8);
            //LoggerInst.Msg($"DEBUG: Texture {tex.name} {BPP[textureFormat]}  {tex.width}   {tex.height}   {mipmaps}  {tex.mipmapCount} -> {bytesCount}");
            VRAMSize += bytesCount;
            if (isActive)
                VRAMSizeOnlyActive += bytesCount;
        }

        private static string ToByteString(long l)
        {
            if (l < 1000) return l + " B";
            if (l < 1000000) return (l / 1000f).ToString("n2") + " KB";
            if (l < 1000000000) return (l / 1000000f).ToString("n2") + " MB";
            else return (l / 1000000000f).ToString("n2") + " GB";
        }
    }
}
