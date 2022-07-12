namespace VRAMChecker
{
    public class AvatarStat
    {
        public string Name, UserID;
        public Result Result;

        public AvatarStat(string name, string userID, Result result)
        {
            Name = name;
            UserID = userID;
            Result = result;
        }

        public string VRAMString => Result.VRAMString;
        public string VRAMActiveOnlyString => Result.VRAMActiveOnlyString;
    }

    public class Result
    {
        public long VRAMMesh, VRAMMeshActiveOnly, VRAMTexture, VRAMTextureActiveOnly;
        public long VRAM => VRAMMesh + VRAMTexture;
        public long VRAMActiveOnly => VRAMMeshActiveOnly + VRAMTextureActiveOnly;
        public string VRAMString => VRAMCheckerInternal.ToByteString(VRAM);
        public string VRAMActiveOnlyString => VRAMCheckerInternal.ToByteString(VRAMActiveOnly);
    }
}
