
using System.ComponentModel;

public class MoveEmailArgs
{
    [Description("The UID of the email to move.")]
    public uint Uid { get; set; }

    [Description("The name of the folder to move the email to.")]
    public string DestinationFolder { get; set; }
}
