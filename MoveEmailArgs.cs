
using System.ComponentModel;

public class MoveEmailArgs
{
    [Description("The UID of the email to move.")]
    public uint Uid { get; set; }

    [Description("The name of the folder to move the email to.")]
    public string DestinationFolder { get; set; }

    [Description("Optional: The source folder to move the email from. If not specified, the inbox will be used.")]
    public string SourceFolder { get; set; }
}
