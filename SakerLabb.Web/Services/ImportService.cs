using System.Diagnostics;
using System.Xml;
using Newtonsoft.Json;
using System.Text.Json; // behöver System.Text.Json för JSON-hantering

namespace SakerLabb.Web.Services;

public class ImportService
{
    private readonly ILogger<ImportService> _logger;
    private readonly HttpClient _http;

    public ImportService(ILogger<ImportService> logger, HttpClient http)
    {
        _logger = logger;
        _http = http;
    }

    public string ImportXml(string xml)
    {
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Parse,
            XmlResolver = new XmlUrlResolver()
        };

        var document = new XmlDocument { XmlResolver = new XmlUrlResolver() };
        using var reader = XmlReader.Create(new StringReader(xml), settings);
        document.Load(reader);

        return document.DocumentElement?.InnerText ?? "";
    }


    //ändrad för att använda System.Text.Json istället för Newtonsoft.Json
    //Vi behöver bara läsa/importera JSON-data. Det finns ingen anledning att
    //låta användaren välja vilka .NET-klasser som ska instansieras.
    public object? ImportJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("JSON får inte vara tom.", nameof(json));
        }

        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    public async Task<string> FetchRemote(string url)
    {
        _logger.LogInformation("Hämtar fjärresurs {Url}", url);
        var response = await _http.GetAsync(url);
        return await response.Content.ReadAsStringAsync();
    }

    //ändrad för att använda ProcessStartInfo istället för Process.Start direkt
    public string Ping(string host)
    {
        if (string.IsNullOrWhiteSpace(host) ||
            Uri.CheckHostName(host) == UriHostNameType.Unknown)
        {
            throw new ArgumentException("Ogiltigt värdnamn.", nameof(host));
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "ping.exe",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add("-n");
        startInfo.ArgumentList.Add("2");
        startInfo.ArgumentList.Add(host);

        using var process = new Process
        {
            StartInfo = startInfo
        };

        process.Start();

        var output = process.StandardOutput.ReadToEnd();

        process.WaitForExit(5000);

        return output;
    }
    }
