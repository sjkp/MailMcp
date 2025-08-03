
using System.ComponentModel;

public class MoveEmailArgs
{
    [Description("The UIDs of the emails to move.")]
    public uint[] Uids { get; set; }

    [Description("The name of the folder to move the email to.")]
    public string DestinationFolder { get; set; }

    [Description("Optional: The source folder to move the email from. If not specified, the inbox will be used.")]
    public string SourceFolder { get; set; }
}
