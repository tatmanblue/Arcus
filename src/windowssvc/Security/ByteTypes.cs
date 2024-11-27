using Microsoft.AspNetCore.Hosting.Server;

namespace ArcusWinSvc.Security;

/// <summary>
/// Copied from an old project of mine, def candidate for refactoring
/// </summary>
[Serializable]
public sealed class FileErasureSettings
{
    public int OverwriteCount { get; set; } = 3;
    public long OverwriteSize { get; set; } = 0L;

    public FileErasureSettings.OverwriteOptions Options { get; set; } = FileErasureSettings.OverwriteOptions.ZeroData;

    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
    [Serializable]
    public class OverwriteOptionsAttributes : Attribute
    {
        private string enumName = string.Empty;
        private string shortName = string.Empty;
        private string desc = string.Empty;
        private bool isOverwriteOption;
        private int recommendMinOverwrite = 1;

        public OverwriteOptionsAttributes(string enumName, string shortName, string desc)
        {
            this.enumName = enumName;
            this.shortName = shortName;
            this.desc = desc;
        }

        public OverwriteOptionsAttributes(
            string enumName,
            string shortName,
            string desc,
            bool isOverwrite)
        {
            this.enumName = enumName;
            this.shortName = shortName;
            this.desc = desc;
            this.isOverwriteOption = isOverwrite;
        }

        public OverwriteOptionsAttributes(
            string enumName,
            string shortName,
            string desc,
            bool isOverwrite,
            int minOverwrite)
        {
            this.enumName = enumName;
            this.shortName = shortName;
            this.desc = desc;
            this.isOverwriteOption = isOverwrite;
            this.recommendMinOverwrite = minOverwrite;
        }

        public override string ToString() => this.ShortName;

        public string Name => this.enumName;

        public string ShortName => this.shortName;

        public string Desc => this.desc;

        public bool IsOverwriteOption => this.isOverwriteOption;

        public int RecommendMinOverwrite => this.recommendMinOverwrite;
    }

    [Flags]
    [Serializable]
    public enum OverwriteOptions
    {
        [FileErasureSettings.OverwriteOptionsAttributes("NotSet", "Not Set",
            "This is an interal value used to default a default")]
        NotSet = 0,

        [FileErasureSettings.OverwriteOptionsAttributes("JustErase", "Just Erase",
            "Erases the file from the system without changing the contents of the file")]
        JustErase = 1,

        [FileErasureSettings.OverwriteOptionsAttributes("Overwrite", "Overwrite",
            "Cause the file contents to be changed prior to erasing the file")]
        Overwrite = 2,

        [FileErasureSettings.OverwriteOptionsAttributes("ZeroData", "Zero Data",
            "Cause the file contents to be changed to all zeros (0) prior to erasing the file", true)]
        ZeroData = 4,

        [FileErasureSettings.OverwriteOptionsAttributes("OneData", "One Data",
            "Cause the file contents to be changed to all ones (1) prior to erasing the file", true)]
        OneData = 16, // 0x00000010

        [FileErasureSettings.OverwriteOptionsAttributes("ZeroOneData", "Zeros and Ones",
            "Cause the file contents to be changed with zeros and ones, prior to erasing the file", true)]
        ZeroOneData = 32, // 0x00000020

        [FileErasureSettings.OverwriteOptionsAttributes("RandomData", "Random",
            "Cause the file contents to be changed with random information, prior to erasing the file", true)]
        RandomData = 64, // 0x00000040

        [FileErasureSettings.OverwriteOptionsAttributes("ZeroToRandomData", "All",
            "Cause the file contents to be changed with all options, prior to erasing the file", true, 4)]
        ZeroToRandomData = RandomData | ZeroOneData | OneData | ZeroData, // 0x00000074

        [FileErasureSettings.OverwriteOptionsAttributes("KeepFile", "Keep File",
            "Cause the file to remain on the system.  Intended for debug use only")]
        KeepFile = 512, // 0x00000200
    }
}

public static class ToggleByte
{
    public static byte b;

    public static byte Toggle()
    {
        ToggleByte.b = ToggleByte.b != (byte)0 ? (byte)0 : byte.MaxValue;
        return ToggleByte.b;
    }
}

public static class RandomByte
{
    private static int seedAIndex = 0;
    private static int seedBIndex = 0;
    private static int seedCIndex = 0;
    public static byte b = 0;

    public static byte[] seedA = new byte[16]
    {
        (byte)1,
        (byte)4,
        (byte)7,
        (byte)10,
        (byte)13,
        (byte)2,
        (byte)5,
        (byte)8,
        (byte)11,
        (byte)14,
        (byte)3,
        (byte)6,
        (byte)9,
        (byte)12,
        (byte)15,
        (byte)30
    };

    public static byte[] seedB = new byte[21]
    {
        (byte)20,
        (byte)169,
        (byte)21,
        (byte)175,
        (byte)28,
        (byte)163,
        (byte)24,
        (byte)4,
        (byte)30,
        (byte)171,
        (byte)18,
        (byte)167,
        (byte)19,
        (byte)173,
        (byte)26,
        (byte)193,
        (byte)214,
        (byte)194,
        (byte)220,
        (byte)192,
        (byte)190
    };

    public static byte[] seedC = new byte[38]
    {
        byte.MaxValue,
        (byte)238,
        (byte)165,
        (byte)191,
        (byte)204,
        (byte)253,
        (byte)236,
        (byte)164,
        (byte)190,
        (byte)203,
        (byte)252,
        (byte)235,
        (byte)163,
        (byte)189,
        (byte)202,
        (byte)250,
        (byte)233,
        (byte)162,
        (byte)188,
        (byte)207,
        (byte)248,
        (byte)231,
        (byte)161,
        (byte)187,
        (byte)205,
        (byte)246,
        (byte)229,
        (byte)160,
        (byte)186,
        (byte)204,
        (byte)244,
        (byte)227,
        (byte)175,
        (byte)176,
        (byte)194,
        (byte)242,
        (byte)225,
        (byte)174
    };

    private static byte GetSeed()
    {
        byte seed = Convert.ToByte((int)RandomByte.seedA[RandomByte.seedAIndex] |
                                   (int)RandomByte.seedB[RandomByte.seedBIndex] |
                                   (int)RandomByte.seedC[RandomByte.seedCIndex]);
        ++RandomByte.seedAIndex;
        if (RandomByte.seedAIndex >= RandomByte.seedA.Length)
            RandomByte.seedAIndex = 0;
        ++RandomByte.seedBIndex;
        if (RandomByte.seedBIndex >= RandomByte.seedB.Length)
            RandomByte.seedBIndex = 0;
        ++RandomByte.seedCIndex;
        if (RandomByte.seedCIndex >= RandomByte.seedC.Length)
            RandomByte.seedCIndex = 0;
        return seed;
    }

    public static byte Random()
    {
        RandomByte.b =
            Convert.ToByte(new System.Random(new System.Random().Next() + (int)RandomByte.GetSeed() + (int)RandomByte.b)
                .Next(0, (int)byte.MaxValue));
        return RandomByte.b;
    }
}