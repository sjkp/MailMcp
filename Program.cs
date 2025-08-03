using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(consoleLogOptions =>
{
    // Configure all logs to go to stderr
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services.AddTransient<MailKit.Net.Imap.ImapClient>(serviceProvider =>
{
    var host = Environment.GetEnvironmentVariable("IMAP_HOST");
    var portString = Environment.GetEnvironmentVariable("IMAP_PORT");
    var username = Environment.GetEnvironmentVariable("IMAP_USERNAME");
    var password = Environment.GetEnvironmentVariable("IMAP_PASSWORD");

    if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(portString) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
    {
        throw new InvalidOperationException("IMAP_HOST, IMAP_PORT, IMAP_USERNAME and IMAP_PASSWORD environment variables must be set.");
    }

    if (!int.TryParse(portString, out var port))
    {
        throw new InvalidOperationException("IMAP_PORT environment variable must be a valid integer.");
    }

    var client = new MailKit.Net.Imap.ImapClient();
    client.Connect(host, port, true);
    client.Authenticate(username, password);
    return client;
});

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

var app = builder.Build();

app.Run();