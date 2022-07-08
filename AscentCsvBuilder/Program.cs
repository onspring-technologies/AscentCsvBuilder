﻿using AscentCsvBuilder.Models;
using CommandDotNet;
using System.Text;
using System.Web;

namespace AscentCsvBuilder;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var appRunner = new AppRunner<Program>();
        var appResult = await appRunner.RunAsync(args);
        return appResult;
    }

    [DefaultCommand]
    public async Task Execute(string companyName, string profileId, string apiToken)
    {
        var regulators = new List<Regulator>();
        var rules = new List<Rule>();
        var tasks = new List<AscentTask>();

        var ascentDataService = new AscentDataService(companyName, profileId, apiToken);
        await FetchRegulatorsWithRules(regulators, rules, ascentDataService);
        await FetchTasks(tasks, ascentDataService);        

        Console.WriteLine("Writing output files");
        OutputFileService.Write(regulators);
        OutputFileService.Write(rules);
        OutputFileService.Write(tasks);

    }

    private static async Task FetchRegulatorsWithRules(List<Regulator> regulators, List<Rule> rules, AscentDataService ascentDataService)
    {
        Console.WriteLine("Fetching regulators");
        var regulatorsResponse = await ascentDataService.GetRegulators();
        foreach (var regulator in regulatorsResponse.Data)
        {
            Console.WriteLine($"Fetching rules for regulator: {regulator.Attributes.Name}");
            regulators.Add(new Regulator(regulator));

            var rulesResponse = await ascentDataService.GetRules(regulator.Id);
            foreach (var rule in rulesResponse.Data)
            {
                rules.Add(new Rule(rule));
            }

            var nextPageUri = rulesResponse.Links.Next;
            var nextPageQueries = HttpUtility.ParseQueryString(nextPageUri.Query);
            var nextPageValue = nextPageQueries["page"];

            while (nextPageValue != null)
            {
                rulesResponse = await ascentDataService.GetByUrl(nextPageUri.ToString());
                foreach (var rule in rulesResponse.Data)
                {
                    rules.Add(new Rule(rule));
                }

                nextPageUri = rulesResponse.Links.Next;
                nextPageQueries = HttpUtility.ParseQueryString(nextPageUri.Query);
                nextPageValue = nextPageQueries["page"];
            }
        }
    }

    private static async Task FetchTasks(List<AscentTask> tasks, AscentDataService ascentDataService)
    {
        Console.WriteLine("Fetching tasks");
        var tasksResponse = await ascentDataService.GetTasks();
        foreach (var task in tasksResponse.Data)
        {
            tasks.Add(new AscentTask(task));
        }

        var nextPageUri = tasksResponse.Links.Next;
        var nextPageQueries = HttpUtility.ParseQueryString(nextPageUri.Query);
        var nextPageValue = nextPageQueries["page"];
        while (nextPageValue != null)
        {
            tasksResponse = await ascentDataService.GetByUrl(nextPageUri.ToString());
            foreach (var task in tasksResponse.Data)
            {
                tasks.Add(new AscentTask(task));
                nextPageUri = tasksResponse.Links.Next;
                nextPageQueries = HttpUtility.ParseQueryString(nextPageUri.Query);
                nextPageValue = nextPageQueries["page"];
            }
        }
    }
}





