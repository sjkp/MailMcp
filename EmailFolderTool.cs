using System;
using System.ComponentModel;
using MailKit.Net.Imap;
using MailKit;
using ModelContextProtocol.Server;
using System.Linq;
using System.Threading.Tasks;

[McpServerToolType]
public static class EmailFolderTool
{
    [McpServerTool, Description("Lists all folders from an IMAP email server.")]
    public static async Task<string> ListFolders(ImapClient client)
    {
        var folders = await client.GetFoldersAsync(client.PersonalNamespaces[0]);

        return string.Join("\n", folders.Select(f => f.FullName));
    }

    [McpServerTool, Description("Creates a new folder on the IMAP server.")]
    public static async Task<string> CreateFolder(ImapClient client, CreateImapFolderArgs args)
    {
        var personalNamespace = client.PersonalNamespaces[0];
        IMailFolder? parentFolder;
        if (string.IsNullOrEmpty(args.ParentFolder))
        {
            parentFolder = client.GetFolder(personalNamespace);
        }
        else
        {
             var folders = await client.GetFoldersAsync(personalNamespace);
             parentFolder = folders.Single(s => s.Name == args.ParentFolder);
        }

        var newFolder = await parentFolder.CreateAsync(args.FolderName, true);
        newFolder.Subscribe();

        return $"Created folder: {newFolder.FullName}";
    }
}
