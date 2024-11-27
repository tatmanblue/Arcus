namespace ArcusWinSvc.Security;

/// <summary>
/// Depending on configuration, delete files with options to overwrite the data first
/// </summary>
public class LocalFileErase
{
    private bool HasOption(
        FileErasureSettings.OverwriteOptions choice,
        FileErasureSettings.OverwriteOptions options)
    {
        return choice == (choice & options);
    }

    private byte GetByte(FileErasureSettings.OverwriteOptions options)
    {
        byte num = 0;
        if (this.HasOption(FileErasureSettings.OverwriteOptions.OneData, options))
            num = (byte)17;         // why 17?
        else if (this.HasOption(FileErasureSettings.OverwriteOptions.RandomData, options))
            num = RandomByte.Random();
        else if (this.HasOption(FileErasureSettings.OverwriteOptions.ZeroOneData, options))
            num = ToggleByte.Toggle();
        return num;
    }

    public void Erase(string fileName)
    {
        if (!File.Exists(fileName))
            throw new FileNotFoundException(fileName, "file doesn't exist");
        if (FileAttributes.Directory == (FileAttributes.Directory & File.GetAttributes(fileName)))
            throw new DirectoryNotFoundException($"cannot open directory for {fileName}");

        if (HasOption(FileErasureSettings.OverwriteOptions.Overwrite, Settings.Options))
        {
            for (int index1 = 0; index1 < Settings.OverwriteCount; ++index1)
            {
                // the reason we check option per iteration is that options is a flag and
                // there could be multiple flags chosen.  Each iteration uses a different option
                FileErasureSettings.OverwriteOptions options = Settings.Options;
                if (HasOption(FileErasureSettings.OverwriteOptions.ZeroToRandomData, Settings.Options))
                {
                    switch (index1 % 4)
                    {
                        case 1:
                            options = FileErasureSettings.OverwriteOptions.OneData;
                            break;
                        case 2:
                            options = FileErasureSettings.OverwriteOptions.ZeroOneData;
                            break;
                        case 3:
                            options = FileErasureSettings.OverwriteOptions.RandomData;
                            break;
                        default:
                            options = FileErasureSettings.OverwriteOptions.ZeroData;
                            break;
                    }
                } 
                
                using FileStream output = File.Open(fileName, FileMode.Open, FileAccess.Write);
                using BinaryWriter binaryWriter = new BinaryWriter((Stream)output);
                long num1 = output.Length + Settings.OverwriteSize;
                for (long index2 = 0; index2 < num1; ++index2)
                {
                    byte num2 = GetByte(options);
                    binaryWriter.Write(num2);
                }

                output.Close();
            }
        }

        if (HasOption(FileErasureSettings.OverwriteOptions.KeepFile, Settings.Options))
            return;
        File.Delete(fileName);
    }

    public string FileName { get; set; } = String.Empty;

    public FileErasureSettings Settings { get; set; } = new();
}