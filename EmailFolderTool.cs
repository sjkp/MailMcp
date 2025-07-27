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
    public static async Task<string> ListFolders()
    {
        var host = Environment.GetEnvironmentVariable("IMAP_HOST");
        var portString = Environment.GetEnvironmentVariable("IMAP_PORT");
        var username = Environment.GetEnvironmentVariable("IMAP_USERNAME");
        var password = Environment.GetEnvironmentVariable("IMAP_PASSWORD");

        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(portString) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            return "Error: IMAP_HOST, IMAP_PORT, IMAP_USERNAME and IMAP_PASSWORD environment variables must be set.";
        }

        if (!int.TryParse(portString, out var port))
        {
            return "Error: IMAP_PORT environment variable must be a valid integer.";
        }

        using var client = new ImapClient();
        await client.ConnectAsync(host, port, true);
        await client.AuthenticateAsync(username, password);

        var folders = await client.GetFoldersAsync(client.PersonalNamespaces[0]);

        await client.DisconnectAsync(true);

        return string.Join("\n", folders.Select(f => f.FullName));
    }

    [McpServerTool, Description("Creates a new folder on the IMAP server.")]
    public static async Task<string> CreateFolder(CreateImapFolderArgs args)
    {
        var host = Environment.GetEnvironmentVariable("IMAP_HOST");
        var portString = Environment.GetEnvironmentVariable("IMAP_PORT");
        var username = Environment.GetEnvironmentVariable("IMAP_USERNAME");
        var password = Environment.GetEnvironmentVariable("IMAP_PASSWORD");

        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(portString) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            return "Error: IMAP_HOST, IMAP_PORT, IMAP_USERNAME and IMAP_PASSWORD environment variables must be set.";
        }

        if (!int.TryParse(portString, out var port))
        {
            return "Error: IMAP_PORT environment variable must be a valid integer.";
        }

        using var client = new ImapClient();
        await client.ConnectAsync(host, port, true);
        await client.AuthenticateAsync(username, password);

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
        await client.DisconnectAsync(true);

        return $"Created folder: {newFolder.FullName}";
    }
}
