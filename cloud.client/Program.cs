using Spectre.Console;
using Spectre.Console.Rendering;
using System.Net.Http.Json;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Cloud.Client
{
    class Program
    {
        static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            var client = new HttpClient { BaseAddress = new Uri("http://localhost:5000") }; // updated to match API

            AnsiConsole.Write(new FigletText("s23 bucket").Color(Color.Blue));
            AnsiConsole.Status()
                .Start("Initialising Client...", ctx =>
                {
                    Thread.Sleep(3000);
                });
            AnsiConsole.MarkupLine("[green]Done![/]");

            //login/reg before opening main menu
            var process = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Continue with")
                    .AddChoices("login", "register", "no authentification"));

            AnsiConsole.MarkupLine($"Proceeding with [blue]{process}[/]");


            if (process != "no authentification")
            {
                var username = AnsiConsole.Ask<string>("username:[/]?");
                var password = AnsiConsole.Prompt(
                    new TextPrompt<string>("password:")
                        .PromptStyle("red")
                        .Secret());
                if (username != null && password != null & process == "login")
                {
                    var response = client.PostAsJsonAsync("auth/login", new { username, password }).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        AnsiConsole.MarkupLine("[green]Login successful![/]");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[red]Login failed![/]");
                        return;
                    }
                }
                else if (username != null && password != null && process == "register")
                {
                    var response = new HttpResponseMessage();
                    try {
                        response = client.PostAsJsonAsync("auth/register", new { username, password }).Result;
                    }
                    catch {
                        throw new Exception("connection to API failed. Make sure the API is running and try again.");
                    }
                    
                    if (response.IsSuccessStatusCode)
                    {
                        AnsiConsole.MarkupLine("[green]Registration successful![/]");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[red]Registration failed![/]");
                        return;
                    }
                }
            }



            AnsiConsole.Clear();
            var layout = new Layout("Root")
                .SplitRows(
                    new Layout("Header").Size(3), // Platz für Logo/Titel
                    new Layout("Main").SplitColumns(
                        new Layout("Tree"),      // Links: Verzeichnisstruktur
                        new Layout("Details")    // Rechts: Datei-Infos/Optionen
                    ),
                    new Layout("Footer").Size(6) // Unten: Eingabefeld/Status
                );

            AnsiConsole.Write(layout);

            //render all layouts below here
        }

        public IRenderable RenderHeader()
        {
            throw new NotImplementedException();
        }
    }
        
}


/*
 layout["Cooling"].Update(RenderMainCooling());
public IRenderable RenderExample(){}

//watch out: let api start 10 sec before
var client = new HttpClient { BaseAddress = new Uri("http://localhost:5000") }; // updated to match API

AnsiConsole.Write(new FigletText("Private Cloud").Color(Color.Blue));
AnsiConsole.Status()
    .Start("Initialising Client...", ctx =>
    {
        Thread.Sleep(3000);
    });
AnsiConsole.MarkupLine("[green]Done![/]");
AnsiConsole.Clear

var layout = new Layout("Root")
    .SplitRows(
        new Layout("Header").Size(3), // Platz für Logo/Titel
        new Layout("Main").SplitColumns(
            new Layout("Tree"),      // Links: Verzeichnisstruktur
            new Layout("Details")    // Rechts: Datei-Infos/Optionen
        ),
        new Layout("Footer").Size(3) // Unten: Eingabefeld/Status
    );

AnsiConsole.Write(layout);


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

*/