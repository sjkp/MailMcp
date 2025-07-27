using System.ComponentModel;

public class CreateImapFolderArgs
{
    [Description("The name of the folder to create.")]
    public string FolderName { get; set; }

    [Description("The parent folder to create the new folder in.")]
    public string? ParentFolder { get; set; }
}