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
    public static async Task<string> ListLastXEmails(ImapClient client, int count = 10)
    {
        await client.Inbox.OpenAsync(FolderAccess.ReadOnly);

        var uids = await client.Inbox.SearchAsync(SearchOptions.All, SearchQuery.All);
        Console.WriteLine($"Found {uids.Count} emails in the inbox.");
        var summaries = await client.Inbox.FetchAsync(uids.UniqueIds.Reverse().Take(count).ToList(), MessageSummaryItems.Envelope);

        return string.Join("\n", summaries.Select(s => $"{s.UniqueId}: {s.Envelope.MessageId}: {s.Envelope.Subject}"));
    }

    [McpServerTool, Description("Get emails from a specific folder.")]
    public static async Task<string> GetEmailsFromFolder(ImapClient client, string folderName)
    {
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
        return string.Join("\n", summaries.Select(s => $"{s.UniqueId}: {s.Envelope.MessageId}: {s.Envelope.Subject}"));
    }

    [McpServerTool, Description("Gets the headers of a specific email by its UID.")]
    public static async Task<string> GetEmailHeaders(ImapClient client, uint uid)
    {
        await client.Inbox.OpenAsync(FolderAccess.ReadOnly);

        var headers = await client.Inbox.GetHeadersAsync(new UniqueId(uid));

        return headers.Select(h => $"{h.Field}: {h.Value}").Aggregate((current, next) => $"{current}{next}");
    }

    [McpServerTool, Description("Moves an email to a specific folder.")]
    public static async Task<string> MoveEmail(ImapClient client, MoveEmailArgs args)
    {
        var folder = await client.GetFolderAsync(args.SourceFolder) ?? client.Inbox;

        await folder.OpenAsync(FolderAccess.ReadWrite);

        var destination = await client.GetFolderAsync(args.DestinationFolder);
        if (destination == null || !destination.Exists)
        {
            return $"Error: Folder '{args.DestinationFolder}' does not exist.";
        }

        await folder.MoveToAsync(args.Uids.Select(uid => new UniqueId(uid)).ToList(), destination);

        return $"Emails with UIDs {string.Join(", ", args.Uids)} moved to folder '{args.DestinationFolder}'.";
    }

    [McpServerTool, Description("Get email body by UID return the HTML body if available, otherwise the text body.")]
    public static async Task<string> GetEmailBody(ImapClient client, uint uid)
    {
        await client.Inbox.OpenAsync(FolderAccess.ReadOnly);

        var message = await client.Inbox.GetMessageAsync(new UniqueId(uid));

        return message.HtmlBody ?? message.TextBody ?? "No body found.";
    }

    [McpServerTool, Description("Searches for emails containing a specific keyword in the body.")]
    public static async Task<string> SearchEmailsByBody(ImapClient client, string keyword)
    {
        await client.Inbox.OpenAsync(FolderAccess.ReadOnly);
        var uids = await client.Inbox.SearchAsync(SearchOptions.All, SearchQuery.BodyContains(keyword));
        if (uids.Count == 0)
        {
            return $"No emails found containing the keyword '{keyword}' in the body.";
        }
        var summaries = await client.Inbox.FetchAsync(uids.UniqueIds, MessageSummaryItems.Envelope);
        return string.Join("\n", summaries.Select(s => $"{s.UniqueId}: {s.Envelope.MessageId}: {s.Envelope.Subject}"));
    }

    [McpServerTool, Description("Searches for emails with a specific subject and potentially author.")]
    public static async Task<string> SearchEmailsBySubjectAndAuthor(ImapClient client, string subject, string? author)
    {
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
        return string.Join("\n", summaries.Select(s => $"{s.UniqueId}: {s.Envelope.MessageId}: {s.Envelope.Subject}"));
    }

    [McpServerTool, Description("Return the MailMCP tool version.")]
    public static string GetVersion()
    {
        //Return the version of the MailMCP tool by readin the assembly version
        return typeof(EmailTool).Assembly.GetName().Version?.ToString() ?? "Unknown version";
    }
}
