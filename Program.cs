using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OaklandCC.Ethos.Common;
using OaklandCC.Ethos.Common.Services;
using OaklandCC.Ethos.eLumen;
using OaklandCC.Ethos.eLumen.Services;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;
using System;
using System.Linq;



var builder = Host.CreateApplicationBuilder(args);

// Parse command line args
var termOptions = new TermOptions();

//to run with command line
if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
{
    termOptions.SelectedTerms = args[0]
        .Split(',', StringSplitOptions.RemoveEmptyEntries)
        .Select(t => t.Trim())
        .ToList();
}
else
{
    Console.WriteLine("No terms entered. Exiting.");
    Environment.Exit(1);
    return;
}

// Proceed with SelectedTerms
Console.WriteLine("Parsed term codes:");
foreach (var term in termOptions.SelectedTerms)
{
    Console.WriteLine(term);
}

// Register services
builder.Services.AddSingleton<Logger>();
builder.Services.AddSingleton<EthosClient>();
builder.Services.AddSingleton<InitService>();
builder.Services.AddSingleton(termOptions);
//builder.Services.AddSingleton<CampusService>();
//builder.Services.AddSingleton<DemographicElementService>();
//builder.Services.AddSingleton<OrgService>();
//builder.Services.AddSingleton<ElumenCourseService>();
//builder.Services.AddSingleton<CouTypeService>();
//builder.Services.AddSingleton<CalendarService>();
//builder.Services.AddSingleton<DemographicCategoryService>();
//builder.Services.AddSingleton<SecattrService>();

builder.Services.AddSingleton<InitService2>();
//builder.Services.AddSingleton<Combine4Services>(); //Offering, OfferingSecattr FacultyAssignment, Enrollment
//builder.Services.AddSingleton<FacAssService>();
//builder.Services.AddSingleton<OfferingService>();
//builder.Services.AddSingleton<OfferingSecattrService>();
builder.Services.AddSingleton<EnrollmentService>();

//builder.Services.AddSingleton<PeopleService>(); //includes demographicData
builder.Services.AddHostedService<Application>();

var host = builder.Build();
host.Run();