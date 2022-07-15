using MelonLoader;
using ReMod.Core.UI.QuickMenu;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using VRC;
using System.Linq;
using UnidecodeSharpFork;
using System.IO;
using AssetsTools.NET;
using TextureFormat = UnityEngine.TextureFormat;
using System.Security.Cryptography;
using System.Text;
using AssetsTools.NET.Extra;
using System.Text.RegularExpressions;

namespace VRAMChecker
{

    public class VRAMCheckerInternal
    {
        public const string Version = "1.0.7";
        private static Regex AssetUrlRegex = new Regex("(file_[0-9A-Za-z-]+)\\/(\\d+)\\/file$");
        public static MelonLogger.Instance LoggerInst;
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
        private static Dictionary<(string id, int version), long> AssetBundleTextures = new Dictionary<(string id, int version), long>();

        private Dictionary<IntPtr, (Texture2D, bool)> textures = new Dictionary<IntPtr, (Texture2D, bool)>();
        private Result result = new Result();
        private bool IgnoreNonActiveTextures = false;
        private VRAMCheckerInternal() { }



        public static void LogInstance()
        {
            LoggerInst.Msg("Vram Sizes of Avatars in Lobby");
            List<AvatarStat> avatarstats = new List<AvatarStat>();

            foreach (Player player in PlayerManager.field_Private_Static_PlayerManager_0.field_Private_List_1_Player_0)
            {
                avatarstats.Add(GetSizeForPlayer(player));
            }

            foreach (var stat in avatarstats.OrderByDescending(x => x.Result.VRAM))
            {
                LoggerInst.Msg($"{stat.Name.Unidecode()}({stat.UserID}) : Total: {stat.VRAMString} OnlyActive; {stat.VRAMActiveOnlyString}");
            }
            LoggerInst.Msg($"Total Avatars : Total: {ToByteString(avatarstats.Sum(x => x.Result.VRAM))} OnlyActive; {ToByteString(avatarstats.Sum(x => x.Result.VRAMActiveOnly))}");
        }

        public static void LogWorld()
        {
            LoggerInst.Msg("Vram Sizes of World Objects");
            long sumVRamSize = 0;
            long sumVRamSizeOnlyActive = 0;
            foreach (var obj in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (obj.GetComponent<Player>() != null)
                    continue;

                var result = new VRAMCheckerInternal().GetSizeForGameObject(obj);
                sumVRamSize += result.VRAM;
                sumVRamSizeOnlyActive += result.VRAMActiveOnly;

                LoggerInst.Msg($"{obj.name}: Total: {result.VRAMString} OnlyActive; {result.VRAMActiveOnlyString}");

            }
            LoggerInst.Msg($"Sum: Total: {ToByteString(sumVRamSize)} OnlyActive; {ToByteString(sumVRamSizeOnlyActive)}");
        }

        public static AvatarStat GetSizeForPlayer(Player player)
        {
            var avatarGameobj = player._vrcplayer.field_Internal_GameObject_0;
            var result = new VRAMCheckerInternal() {IgnoreNonActiveTextures = true}.GetSizeForGameObject(avatarGameobj);
            result.VRAMTexture = GetTextureSizeAssetBundle(player, true);
            return new AvatarStat(player.field_Private_APIUser_0.displayName, player.field_Private_APIUser_0.id, result);
        }

        public Result GetSizeForGameObject(GameObject avatar)
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

            foreach (var item in textures)
            {
                GetSizeForTexture(item.Value.Item1, item.Value.Item2);
            }

            return result;
        }

        private void GetSizeForRenderer(SkinnedMeshRenderer renderer)
        {
            foreach (var mat in renderer.materials)
            {
                CollectMaterial(mat, renderer.gameObject.activeInHierarchy);
            }
            GetSizeForMesh(renderer.sharedMesh, renderer.gameObject.activeInHierarchy);
        }

        private void GetSizeForRenderer(MeshRenderer renderer)
        {
            foreach (var mat in renderer.materials)
            {
                CollectMaterial(mat, renderer.gameObject.activeInHierarchy);
            }
        }

        private void GetSizeForMesh(Mesh mesh, bool isActive)
        {
            if (mesh == null) return;
            var total = Profiler.GetRuntimeMemorySizeLong(mesh);
            //LoggerInst.Msg($"DEBUG: Mesh: {mesh.name} Profiler.GetRuntimeMemorySizeLong(mesh): {total}");
            result.VRAMMesh += total;
            if (isActive)
                result.VRAMMeshActiveOnly += total;
        }

        private void CollectMaterial(Material mat, bool isActive)
        {
            if (!isActive && IgnoreNonActiveTextures)
                return;

            if (mat == null) return;
            var texIds = mat.GetTexturePropertyNames();
            foreach (var id in texIds)
            {
                try
                {
                    var test = mat.GetTexture(id);
                    if (test == null)
                        continue;
                    CollectTexture(test.Cast<Texture2D>(), isActive);
                }
                catch (Exception)
                {
                }
            }
        }

        private void CollectTexture(Texture2D tex, bool isActive)
        {
            if (tex == null) return;
            if (!textures.ContainsKey(tex.Pointer))
            {
                textures[tex.Pointer] = (tex, isActive);
                return;
            }
            if (!textures[tex.Pointer].Item2)
            {
                textures[tex.Pointer] = (tex, isActive);
            }

        }

        private void GetSizeForTexture(Texture2D tex, bool isActive)
        {
            TextureFormat textureFormat = tex.format;

            if (!BPP.ContainsKey(tex.format))
            {
                LoggerInst.Warning("Does not have BPP for " + textureFormat);
                return;
            }

            var bytesCount = GetSizeForTexture(BPP[textureFormat], tex.width, tex.height, tex.mipmapCount);
            //LoggerInst.Msg($"DEBUG: Texture {tex.name} {BPP[textureFormat]}  {tex.width}   {tex.height}   {mipmaps}  {tex.mipmapCount} -> {bytesCount}");
            result.VRAMTexture += bytesCount;
            if (isActive)
                result.VRAMTextureActiveOnly += bytesCount;
        }

        private static long GetSizeForTexture(long bbp, long width, long height, int mipCount)
        {
            double mipmaps = 1;
            for (int i = 0; i < mipCount; i++) mipmaps += Math.Pow(0.25, i + 1);

            var bytesCount = bbp * width * height / 8L;
            bytesCount = (long)(bytesCount * Math.Round(mipmaps, 6));
            //LoggerInst.Msg($"DEBUG: Texture {tex.name} {BPP[textureFormat]}  {tex.width}   {tex.height}   {mipmaps}  {tex.mipmapCount} -> {bytesCount}");
            return bytesCount;
        }

        public static long GetTextureSizeAssetBundle(Player player, bool logMissingFile = false)
        {
            var match = AssetUrlRegex.Match(player.prop_ApiAvatar_0.assetUrl);
            if(!match.Success)
                return 0;

            return GetTextureSizeAssetBundle(match.Groups[1].Value, Convert.ToInt32(match.Groups[2].Value), logMissingFile);
        }

        private static long GetTextureSizeAssetBundle(string id, int version, bool logMissingFile)
        {
            if (AssetBundleTextures.ContainsKey((id, version)))
                return AssetBundleTextures[(id, version)];
            try
            {
                var file = $"{AssetBundleDownloadManager.field_Private_Static_AssetBundleDownloadManager_0.field_Private_Cache_0.path}/{GetAssetId(id)}/{GetAssetVersion(version)}/__data";

                if (!File.Exists(file))
                {
                    if(logMissingFile)
                        LoggerInst.Msg($"Failed to load {GetAssetId(id)}/{GetAssetVersion(version)}");
                    return 0;
                }

                var am = new AssetsManager();
                var bun = am.LoadBundleFile(DecompressAssetBundle(File.OpenRead(file)), "__data");
                var assetInst = am.LoadAssetsFileFromBundle(bun, 0, true);
                var totalsize = 0L;
                foreach (var item in assetInst.table.GetAssetsOfType((int)AssetClassID.Texture2D))
                {
                    var test = am.GetTypeInstance(assetInst, item).GetBaseField();

                    var bpp = BPP[(TextureFormat)test.Get("m_TextureFormat").value.AsInt()];
                    var width = test.Get("m_Width").value.AsInt();
                    var height = test.Get("m_Height").value.AsInt();
                    var mipCount = test.Get("m_MipCount").value.AsInt();

                    totalsize += GetSizeForTexture(bpp, width, height, mipCount);
                }

                LoggerInst.Msg($"Preloaded texture size for {GetAssetId(id)}/{GetAssetVersion(version)} with size {ToByteString(totalsize)}");
                AssetBundleTextures[(id, version)] = totalsize;
                return totalsize;
            }
            catch (Exception)
            {
                LoggerInst.Msg($"Failed to load {GetAssetId(id)}/{GetAssetVersion(version)}");
                return 0;
            }
        }

        //https://github.com/Natsumi-sama/VRCX/blob/PyPyDanceCompanion/AssetBundleCacher.cs#L35
        private static string GetAssetId(string id)
        {
            byte[] hash = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(id));
            StringBuilder idHex = new StringBuilder(hash.Length * 2);
            foreach (byte b in hash)
            {
                idHex.AppendFormat("{0:x2}", b);
            }
            return idHex.ToString().ToUpper().Substring(0, 16);
        }

        //https://github.com/Natsumi-sama/VRCX/blob/PyPyDanceCompanion/AssetBundleCacher.cs#L46
        private static string GetAssetVersion(int version)
        {
            byte[] bytes = BitConverter.GetBytes(version);
            string versionHex = String.Empty;
            foreach (byte b in bytes)
            {
                versionHex += b.ToString("X2");
            }
            return versionHex.PadLeft(32, '0');
        }

        private static Stream DecompressAssetBundle(Stream stream)
        {
            //AssetTools doesnt recognize LZ4 Runtime so we manually decompress it
            var file = new AssetBundleFile();
            var resultStream = new MemoryStream();


            file.Read(new AssetsFileReader(stream), true);
            file.reader.Position = 0L;
            file.Unpack(file.reader, new AssetsFileWriter(resultStream));

            resultStream.Position = 0;

            return resultStream;
        }

        public static string ToByteString(long l)
        {
            if (l < 1000) return l + " B";
            if (l < 1000000) return (l / 1000f).ToString("n2") + " KB";
            if (l < 1000000000) return (l / 1000000f).ToString("n2") + " MB";
            else return (l / 1000000000f).ToString("n2") + " GB";
        }
    }
}
