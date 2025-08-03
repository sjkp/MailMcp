using System;
using System.ComponentModel;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit;
using ModelContextProtocol.Server;

[McpServerToolType]
public static class EmailTool
{
    [McpServerTool, Description("Lists the last X emails from an IMAP email server.")]
    public static async Task<string> ListLastXEmails(int count = 10)
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

        var uids = await client.Inbox.SearchAsync(SearchOptions.All, SearchQuery.All);
        Console.WriteLine($"Found {uids.Count} emails in the inbox.");
        var summaries = await client.Inbox.FetchAsync(uids.UniqueIds.Reverse().Take(count).ToList(), MessageSummaryItems.Envelope);

        await client.DisconnectAsync(true);

        return string.Join("\n", summaries.Select(s => $"{s.UniqueId}: {s.Envelope.MessageId}: {s.Envelope.Subject}"));
    }

    [McpServerTool, Description("Get emails from a specific folder.")]
    public static async Task<string> GetEmailsFromFolder(string folderName)
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
        var folder = await client.GetFolderAsync(folderName);
        if (folder == null || !folder.Exists)
        {
            return $"Error: Folder '{folderName}' does not exist.";
        }

        await folder.OpenAsync(FolderAccess.ReadOnly);
        var uids = await folder.SearchAsync(SearchOptions.All, SearchQuery.All);
        if (uids.Count == 0)
        {
            return $"No emails found in folder '{folderName}'.";
        }

        var summaries = await folder.FetchAsync(uids.UniqueIds, MessageSummaryItems.Envelope);
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
        var folder = await client.GetFolderAsync(args.SourceFolder) ?? client.Inbox;

        await folder.OpenAsync(FolderAccess.ReadWrite);

        var destination = await client.GetFolderAsync(args.DestinationFolder);
        if (destination == null || !destination.Exists)
        {
            return $"Error: Folder '{args.DestinationFolder}' does not exist.";
        }

        await folder.MoveToAsync(new UniqueId(args.Uid), destination);

        await client.DisconnectAsync(true);

        return $"Email with UID {args.Uid} moved to folder '{args.DestinationFolder}'.";
    }

    [McpServerTool, Description("Get email body by UID return the HTML body if available, otherwise the text body.")]
    public static async Task<string> GetEmailBody(uint uid)
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

        var message = await client.Inbox.GetMessageAsync(new UniqueId(uid));

        await client.DisconnectAsync(true);

        return message.HtmlBody ?? message.TextBody ?? "No body found.";
    }

    [McpServerTool, Description("Searches for emails containing a specific keyword in the body.")]
    public static async Task<string> SearchEmailsByBody(string keyword)
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
        var uids = await client.Inbox.SearchAsync(SearchOptions.All, SearchQuery.BodyContains(keyword));
        if (uids.Count == 0)
        {
            return $"No emails found containing the keyword '{keyword}' in the body.";
        }
        var summaries = await client.Inbox.FetchAsync(uids.UniqueIds, MessageSummaryItems.Envelope);
        await client.DisconnectAsync(true);
        return string.Join("\n", summaries.Select(s => $"{s.UniqueId}: {s.Envelope.MessageId}: {s.Envelope.Subject}"));
    }

    [McpServerTool, Description("Searches for emails with a specific subject and potentially author.")]
    public static async Task<string> SearchEmailsBySubjectAndAuthor(string subject, string? author)
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
        SearchQuery query = SearchQuery.SubjectContains(subject);
        if (!string.IsNullOrEmpty(author))
        {
            query = query.And(SearchQuery.FromContains(author));
        }
        var uids = await client.Inbox.SearchAsync(SearchOptions.All, query);

        if (uids.Count == 0)
        {
            return $"No emails found with subject '{subject}' from author '{author}'.";
        }

        var summaries = await client.Inbox.FetchAsync(uids.UniqueIds, MessageSummaryItems.Envelope);
        await client.DisconnectAsync(true);
        return string.Join("\n", summaries.Select(s => $"{s.UniqueId}: {s.Envelope.MessageId}: {s.Envelope.Subject}"));
    }

    [McpServerTool, Description("Return the MailMCP tool version.")]
    public static string GetVersion()
    {
        //Return the version of the MailMCP tool by readin the assembly version
        return typeof(EmailTool).Assembly.GetName().Version?.ToString() ?? "Unknown version";
    }
}
