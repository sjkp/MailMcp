using System;
using System.ComponentModel;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit;
using ModelContextProtocol.Server;

[McpServerToolType]
public static class EmailTool
{
    [McpServerTool, Description("Lists the last 10 emails from an IMAP email server.")]
    public static async Task<string> ListLast10Emails()
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
        await client.Inbox.OpenAsync(FolderAccess.ReadOnly);

        var uids = await client.Inbox.SearchAsync(SearchQuery.All);
        var summaries = await client.Inbox.FetchAsync(uids.Reverse().Take(10).ToList(), MessageSummaryItems.Envelope);

        await client.DisconnectAsync(true);

        return string.Join("\n", summaries.Select(s => $"{s.UniqueId}: {s.Envelope.MessageId}: {s.Envelope.Subject}"));
    }

    [McpServerTool, Description("Gets the headers of a specific email by its UID.")]
    public static async Task<string> GetEmailHeaders(uint uid)
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
        await client.Inbox.OpenAsync(FolderAccess.ReadOnly);

        var headers = await client.Inbox.GetHeadersAsync(new UniqueId(uid));

        await client.DisconnectAsync(true);

        return headers.Select(h => $"{h.Field}: {h.Value}").Aggregate((current, next) => $"{current}\n{next}");
    }

    [McpServerTool, Description("Moves an email to a specific folder.")]
    public static async Task<string> MoveEmail(MoveEmailArgs args)
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
        await client.Inbox.OpenAsync(FolderAccess.ReadWrite);

        var destination = client.GetFolder(args.DestinationFolder);
        if (destination == null)
        {
            return $"Error: Folder '{args.DestinationFolder}' not found.";
        }

        await client.Inbox.MoveToAsync(new UniqueId(args.Uid), destination);

        await client.DisconnectAsync(true);

        return $"Email with UID {args.Uid} moved to folder '{args.DestinationFolder}'.";
    }
}
