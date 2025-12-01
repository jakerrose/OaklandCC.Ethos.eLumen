using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using OaklandCC.Ethos.Common;
using OaklandCC.Ethos.Common.Models;
using OaklandCC.Ethos.Common.Services;
using static System.Collections.Specialized.BitVector32;

namespace OaklandCC.Ethos.eLumen.Services
{
    public class InitService
    {
        private readonly EthosClient _ethosClient;
        private readonly IConfiguration _configuration;
        private readonly Logger _logger;
        public List<string> SelectedTerms { get; private set; } = new();


        /// <summary>  
        /// Initializes a new instance of the <see cref="InitService"/> class.  
        /// </summary>  
        /// <param name="ethosClient">The Ethos client for API interactions.</param>  
        /// <param name="configuration">The application configuration settings.</param>  
        /// <param name="logger">The logger for logging messages.</param>  
        public InitService(EthosClient ethosClient, IConfiguration configuration, Logger logger)
        {
            _ethosClient = ethosClient;
            _configuration = configuration;
            _logger = logger;
        }
        public async Task CreateInitFiles()
        {
            //comment out json files already created

            //to start with 'run'
            //Console.WriteLine("Enter up to 5 term codes separated by commas (e.g., 2024/FA,2025/SU):");
            //var input = Console.ReadLine();

            //if (!string.IsNullOrWhiteSpace(input))
            //{
            //    SelectedTerms = input
            //        .Split(',', StringSplitOptions.RemoveEmptyEntries)
            //        .Select(t => t.Trim())
            //        .ToList();
            //}
            //else
            //{
            //    Console.WriteLine("No terms entered. Exiting.");
            //    Environment.Exit(1);
            //}
            //To type in today's date
            //Console.WriteLine("Please enter today's date in this format: 2025-05-09");
            //var dateInput = Console.ReadLine() ?? string.Empty;
            //UserInput.StoredValue2 = dateInput;
            //Console.WriteLine(dateInput);

            //To use DateTime
            var currentDate = DateTime.Today.ToString().Split(" ")[0];
            string[] currentDateParts = currentDate.Split("/");
            var curYear = currentDateParts[2];
            var curMonth = currentDateParts[0];
            if (curMonth.Length == 1)
            {
                curMonth = $"0{curMonth}";
            }
            var curDay = currentDateParts[1];
            if (curDay.Length == 1)
            {
                curDay = $"0{curDay}";
            }
            var dateInput = $"{curYear}-{curMonth}-{curDay}";
            Console.WriteLine("Today's date: " + dateInput);

            //Console.WriteLine("Creating educational units file, please wait");
            //var eduService = new EducationalInstitutionUnitService(_ethosClient);
            //var allUnits = await eduService.GetAllUnitsAsync();
            //var json2 = System.Text.Json.JsonSerializer.Serialize(allUnits, new JsonSerializerOptions { WriteIndented = true });
            //await File.WriteAllTextAsync("units.json", json2);
            //Console.WriteLine("Wrote json file for all units");

            ////dateInput = current date
            //Console.WriteLine("Creating courses file by active date, please wait");
            //var courseService = new CourseSectionService(_ethosClient);
            //var allCourses = await courseService.GetAllCoursesPagedAsync(dateInput);
            //var json3 = System.Text.Json.JsonSerializer.Serialize(allCourses, new JsonSerializerOptions { WriteIndented = true });
            //await File.WriteAllTextAsync("courses.json", json3);
            //Console.WriteLine("Wrote json file for courses by active date");

            //Console.WriteLine("Creating all subjects files, please wait");
            //var subjectService = new SubjectService(_ethosClient, _logger);
            //var allSubjects = await subjectService.GetAllSubjectsAsync();
            //var json4 = System.Text.Json.JsonSerializer.Serialize(allSubjects, new JsonSerializerOptions { WriteIndented = true });
            //await File.WriteAllTextAsync("subjects.json", json4);
            //Console.WriteLine("Wrote json file for all subjects");

            //Console.WriteLine("Creating all instructional methods file, please wait");
            //var methodService = new InstructionalMethodService(_ethosClient);
            //var allMethods = await methodService.GetAllMethodsAsync();
            //var json5 = System.Text.Json.JsonSerializer.Serialize(allMethods, new JsonSerializerOptions { WriteIndented = true });
            //await File.WriteAllTextAsync("methods.json", json5);
            //Console.WriteLine("Wrote json file for all methods");

            //Console.WriteLine("Creating all sites file, please wait");
            //var siteService = new SitesService(_ethosClient);
            //var allSites = await siteService.GetAllSitesAsync();
            //var json6 = System.Text.Json.JsonSerializer.Serialize(allSites, new JsonSerializerOptions { WriteIndented = true });
            //await File.WriteAllTextAsync("sites.json", json6);
            //Console.WriteLine("Wrote json file for all sites");

            //Console.WriteLine("Creating all grade definitions file, please wait");
            //var gradeService = new GradeService(_ethosClient);
            //var gradeDefs = await gradeService.GetAllGradeDefinitionsAsync();
            //var json8 = System.Text.Json.JsonSerializer.Serialize(gradeDefs, new JsonSerializerOptions { WriteIndented = true });
            //await File.WriteAllTextAsync("gradeDefs.json", json8);
            //Console.WriteLine("Wrote json file for grade definitions");

        }
    }
}
