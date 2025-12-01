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
    /// <summary>  
    /// This class provides functionality to create a campus file.  
    /// It retrieves the file path from configuration settings and processes the data accordingly.  
    /// </summary>  
    public class InitService2
    {
        private readonly EthosClient _ethosClient;
        private readonly IConfiguration _configuration;
        private readonly Logger _logger;
        public List<string> SelectedTerms { get; private set; } = new();


        /// <summary>  
        /// Initializes a new instance of the <see cref="InitService2"/> class.  
        /// </summary>  
        /// <param name="ethosClient">The Ethos client for API interactions.</param>  
        /// <param name="configuration">The application configuration settings.</param>  
        /// <param name="logger">The logger for logging messages.</param>  
        public InitService2(EthosClient ethosClient, IConfiguration configuration, Logger logger)
        {
            _ethosClient = ethosClient;
            _configuration = configuration;
            _logger = logger;
        }
        public async Task CreateInit2Files(string term)
        {
            UserInput.StoredAcadCode = term;
            var acadService = new AcademicPeriodService2(_ethosClient, _logger);
            var academicPeriodId = "";
            //var activeDate = "";
            List<AcademicPeriod2> periods = await acadService.GetPeriodsByCode(term);
            foreach (var period in periods)
            {
                academicPeriodId = period.id;
            }
            UserInput.StoredAcadPeriod = academicPeriodId;

            //to record all staff Ids
            //Console.WriteLine("Gathering all active employee Ids for later use, please wait");
            //var employeeService = new EmployeeService(_ethosClient, _logger);
            //var personService = new PersonService(_ethosClient);
            //var activeEmps = await employeeService.GetActiveEmployeesAsync();
            //var service = new InitiationService();
            ////var uniqueLines = new HashSet<string>();
            //var techids = new List<TechIds>();
            //List<Employee> employees = new List<Employee>();

            ////foreach (var employee in activeEmps)
            //for (int i = 0; i < activeEmps.Count; i++)
            //{
            //    var employee = activeEmps[i];
            //    Console.WriteLine("Currently processing number " + i + " of " + activeEmps.Count);
            //    var techId = "";
            //    var id = employee.person.id;

            //    Person person = await personService.GetPersonByIdAsync(id);
            //    if (person.credentials != null)
            //    {
            //        foreach (var credential in person.credentials)
            //        {
            //            switch (credential.type)
            //            {
            //                case "colleaguePersonId":
            //                    techId = credential.value;
            //                    break;
            //            }
            //        }
            //    }

            //    string line = $"{id},{techId}";

            //    if (service.uniqueLines.Add(line)) // Add returns false if already exists
            //    {
            //        techids.Add(new TechIds { guid = id, colleagueId = techId });
            //    }
            //}
            //string filePath2 = "techids.csv";
            //using (var writer = new StreamWriter(filePath2, append: false))
            //{
            //    writer.WriteLine("guid,colleagueId"); // header

            //    foreach (var line in service.uniqueLines)
            //    {
            //        writer.WriteLine(line);
            //    }
            //    writer.Flush();

            //    Console.WriteLine("wrote techids.csv file with first batch of emp Ids");
            //}

            ////to record more staff Ids
            //Console.WriteLine("Gathering more employee Ids for later use, please wait");

            ////var excludedIds = new HashSet<string>(
            ////    System.Text.Json.JsonSerializer.Deserialize<List<string>>(File.ReadAllText("ExtraIds.json"))
            ////);
            //var excludedIds = ExcludedIds.Values;
            //var newLines = new List<string>();

            //var emmaEmps = await employeeService.GetAllEmployeesCustomAsync();
            ////var uniqueLines = new HashSet<string>();

            //for (int e = 0; e < emmaEmps.Count; e++)
            //{
            //    var employee = emmaEmps[e];
            //    Console.WriteLine("Currently processing number " + e + " of " + emmaEmps.Count);
            //    var techId = "";
            //    var empId = employee.id;
            //    techId = employee.colleagueId;
            //    if (excludedIds.Contains(techId)) continue;

            //    var id = "";
            //    List<PersonColleagueId> personz = await personService.GetPersonByColleagueIdAsync(techId);
            //    foreach (var person in personz)
            //    {
            //        id = person.id;
            //    }

            //    string line2 = $"{id},{techId}";

            //    if (service.uniqueLines.Add(line2)) // Add returns false if already exists
            //    {
            //        techids.Add(new TechIds { guid = id, colleagueId = techId });
            //        newLines.Add(line2);
            //    }
            //}
            //using (var writer2 = new StreamWriter(filePath2, append: true))
            //{
            //    foreach (var linez in newLines)
            //    {

            //        writer2.WriteLine(linez);
            //    }
            //    writer2.Flush();
            //}
            //Console.WriteLine("wrote techids.csv file with 2nd batch of empIds");



            ////comment out json files already created
            //Console.WriteLine("Creating section registrations file for term " + academicPeriodId);
            //var secRegService = new SectionRegistrationService(_ethosClient);
            //var allSecReg = await secRegService.GetAllSectionRegistrationsByAcademicPeriodIdAsync(academicPeriodId);
            //var json8 = System.Text.Json.JsonSerializer.Serialize(allSecReg, new JsonSerializerOptions { WriteIndented = true });
            //await File.WriteAllTextAsync("sectionRegistrations.json", json8);
            //Console.WriteLine("Wrote json file for all section registrations by academic period");

            //Console.WriteLine("Creating sections file for term " + academicPeriodId);
            //var sectionService = new CourseSectionService(_ethosClient);
            ////get json file of all sections in academic period
            //var allSections = await sectionService.GetSectionsByPeriodAsync(academicPeriodId);
            //var json = System.Text.Json.JsonSerializer.Serialize(allSections, new JsonSerializerOptions { WriteIndented = true });
            //await File.WriteAllTextAsync("sections.json", json);
            //Console.WriteLine("Wrote json file for section by academic period");

            //Console.WriteLine("Creating instructional events file for term " + academicPeriodId);
            //var eventService = new InstructionalEventService(_ethosClient);
            //var allEvents = await eventService.GetAllEventsByAcademicPeriodIdAsync(academicPeriodId);
            //var json7 = System.Text.Json.JsonSerializer.Serialize(allEvents, new JsonSerializerOptions { WriteIndented = true });
            //await File.WriteAllTextAsync("events.json", json7);
            //Console.WriteLine("Wrote json file for all instructional events");

            //Console.WriteLine("Creating instructional events file by section Ids, please wait");
            //var eventsService = new InstructionalEventService(_ethosClient);
            //var sections = JsonConvert.DeserializeObject<List<SectionById>>(File.ReadAllText("./sections.json"));

            //var allEventSections = new List<InstructionalEventById>();

            //for (int s = 0; s < sections.Count; s++)
            //{
            //    var section = sections[s];
            //    Console.WriteLine("Currently processing section number " + s);
            //    var sectionId = section.id;
            //    var eventSections = await eventsService.GetInstructionalEventsBySectionIdAsync(sectionId);

            //    if (eventSections != null)
            //    {
            //        allEventSections.AddRange(eventSections);
            //    }
            //}

            //// Write all collected event sections as a JSON array
            //var outputJson = JsonConvert.SerializeObject(allEventSections, Formatting.Indented);
            //await File.WriteAllTextAsync("eventSections.json", outputJson);
            //Console.WriteLine("Wrote json file for all instructional events by section");

            //Console.WriteLine("Creating person file for academic period " + academicPeriodId);
            ////var personService = new PersonService(_ethosClient);
            //var secRegList = JsonConvert.DeserializeObject<List<SectionRegistration>>(File.ReadAllText("./sectionRegistrations.json"));
            //var allPersons = new List<Person>();

            //for (int p = 0; p < secRegList.Count; p++)
            //{
            //    var secReg = secRegList[p];
            //    Console.WriteLine("index: " + p);
            //    var id = secReg.registrant.id;
            //    var students = await personService.GetPersonByIdAsync(id);

            //    if (students != null)
            //    {
            //        allPersons.Add(students);
            //    }
            //}
            //// Write all collected event sections as a JSON array
            //var outputPersonJson = JsonConvert.SerializeObject(allPersons, Formatting.Indented);
            //await File.WriteAllTextAsync("persons.json", outputPersonJson);
            //Console.WriteLine("Wrote json file for all persons by section registration");
        }
    }
}


