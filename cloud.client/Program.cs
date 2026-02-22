using Spectre.Console;
using System.Net.Http.Json;

//Console.ReadLine();
Thread.Sleep(7000); // Wait for API to start up (in a real app, you'd want a more robust solution)
var client = new HttpClient { BaseAddress = new Uri("http://localhost:5000") }; // updated to match API

AnsiConsole.Write(new FigletText("Private Cloud").Color(Color.Blue));

// 1. Fetch files from API
var files = await client.GetFromJsonAsync<List<FileEntryDto>>("files");

// 2. Show Selection Menu
var choice = AnsiConsole.Prompt(
    new SelectionPrompt<FileEntryDto>()
        .Title("Which file do you want to [green]view[/]?")
        .AddChoices(files)
        .UseConverter(f => f.Name));

// 3. "Download" and print content
var content = await client.GetStringAsync($"files/{choice.Id}");
AnsiConsole.MarkupLine($"[yellow]File Content:[/]\n{content}");

public record FileEntryDto(
    string Id,
    string Name,
    long Size,
    string Path,
    bool IsPublic,
    string OwnerId
    );