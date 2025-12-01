using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using OaklandCC.Ethos.Common.Models;
using OaklandCC.Ethos.Common.Services;
using OaklandCC.Ethos.Common;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

namespace OaklandCC.Ethos.eLumen.Services
{
    public class FacAssService
    {
        private readonly EthosClient _ethosClient;
        private readonly IConfiguration _configuration;
        private readonly Logger _logger;

        /// <summary>  
        /// Initializes a new instance of the <see cref="FacAssService"/> class.  
        /// </summary>  
        /// <param name="ethosClient">The Ethos client for API interactions.</param>  
        /// <param name="configuration">The application configuration settings.</param>  
        /// <param name="logger">The logger for logging messages.</param>  
        public FacAssService(EthosClient ethosClient, IConfiguration configuration, Logger logger)
        {
            _ethosClient = ethosClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task CreateFacAssFile()
        {
            //var techids = new List<TechIds>();
            var uniqueLines = new HashSet<string>();
            var service = new InitiationService();
            //To ensure no duplicate rows
            HashSet<string> facValues = new HashSet<string>();

            List<SectionById> sectionList = new List<SectionById>();
            var json = File.ReadAllText("./sections.json");
            sectionList = JsonConvert.DeserializeObject<List<SectionById>>(json);

            //******************To write FACULTYASSIGNMENT.csv
            //services
            var facultyService = new FacultyService(_ethosClient);
            var personService = new PersonService(_ethosClient);

            List<SectionRegistration> secRegList = new List<SectionRegistration>();
            var json6 = File.ReadAllText("./sectionRegistrations.json");
            secRegList = JsonConvert.DeserializeObject<List<SectionRegistration>>(json6);

            // Retrieve file path from appsettings.json  
            string? filePath = _configuration["FileSettings:FacAssOutputFilePath"];
            if (string.IsNullOrEmpty(filePath))
            {
                _logger.LogString("Error: File path is not configured in appsettings.json.", LogEntryType.Error);
                return;
            }
            _logger.LogString($"FacAssOutputFilePath: {filePath}:", LogEntryType.Info);

            // Retrieve section 
            //Fall 2023
            var academicPeriodId = "b901962d-2f32-4050-b09c-de714a81ea7f";

            //get start on date
            var acPerService = new AcademicPeriodService2(_ethosClient, _logger);
            AcademicPeriod2 acadObject = await acPerService.GetAcademicPeriodByIdAsync(academicPeriodId);
            var termDropStart = acadObject.termDropStartDate.Replace("-", "");
            int dropStart = Int32.Parse(termDropStart);

            using (StreamWriter writer = new StreamWriter(filePath, append: false))
            {
                // Write header line  
                await writer.WriteLineAsync("COU_ID,YRTR,TECH_ID");

                string filePath2 = "techids.csv";

                // Optional: add header line
                using (var writer2 = new StreamWriter(filePath2, append: true))
                {
                    // Process each location  
                    for (int i = 0; i < secRegList.Count; i++)
                    {
                        Console.WriteLine("index: " + i);
                        var secReg = secRegList[i];

                        //to get DROP_TIME_STAMP
                        //bool droppedEarly = false;
                        var droppedDate = "";
                        var dropStatus = secReg.status.sectionRegistrationStatusReason;
                        if (dropStatus == "dropped" || dropStatus == "withdrawn")
                        {
                            droppedDate = secReg.statusDate.Replace("-", "");

                        }

                        //section registration statuses
                        var N = "d016f524-b63e-4821-801d-82bea77d777d";
                        var A = "581cae92-99e0-4ba8-a391-f329ddcdfc1a";
                        var D = "724aba8e-cfce-4234-87eb-9ce5c13ac6aa";
                        var W = "1ebd4fff-8171-412f-878c-0c0e03dd3bc2";
                        var X = "9d25bb16-cca9-4516-89a0-34d271723e46";
                        var C = "3284bf83-0297-4616-8c55-c5215c2bf24f";
                        var DR = "7a77a59e-990b-429b-a61a-a07e3233c737";

                        var secRegStausDetail = secReg.status.detail.id;
                        bool dropEarly = false;

                        bool badStatus = false;
                        if (secRegStausDetail == X || secRegStausDetail == C || secRegStausDetail == DR)
                        {
                            badStatus = true;
                        }
                        if (!badStatus)
                        {
                            //check secReg status
                            if (secRegStausDetail == W || secRegStausDetail == D)
                            {
                                int droppedDateNum = Int32.Parse(droppedDate);
                                if (droppedDateNum < dropStart)
                                {
                                    //bypass
                                    dropEarly = true;
                                }
                            }

                            if (dropEarly == false)
                            {

                                //**************************To get techID

                                var sectionId = secReg.section.id;
                                var id = secReg.registrant.id;

                                List<Faculty> facultyList = await facultyService.GetInstructors(sectionId: $"{sectionId}", limit: 100);
                                //more than one techIds are sometimes found
                                List<string> facIds = new List<string>();

                                foreach (var faculty in facultyList)
                                {
                                    if (faculty.facultyId != null)
                                    {
                                        facIds.Add(faculty.facultyId);

                                    }
                                }
                                if (facIds.Count > 0)
                                {

                                    //call Section by Id to get couId and Term
                                    var cancelled = "e8ab394d-b5bf-42bd-834a-3e973664eb48";
                                    //credit categories
                                    var CRE = "68ac1a07-440a-4830-99e6-a19b4263f3a8";
                                    var DEV = "f1d869b2-8920-471b-8c6c-b4b1fab05283";

                                    bool isCancelled = false;
                                    var couId = "";
                                    var Yrtr = "";
                                    var Year = "";
                                    var status = "";
                                    var creditType = "";

                                    foreach (var section in sectionList)
                                    {
                                        if (sectionId == section.id)
                                        {
                                            //check instructional methods for cancelled

                                            if (section.instructionalMethods != null)
                                            {
                                                foreach (var method in section.instructionalMethods)
                                                {
                                                    if (method.id == cancelled)
                                                    {
                                                        isCancelled = true;
                                                        break;
                                                    }

                                                }
                                            }
                                            if (section.courseSectionsId != null)
                                            { couId = section.courseSectionsId; }


                                            if (section.termCode != null)
                                            {
                                                var yrtr = section.termCode;
                                                string[] yrtrParts = yrtr.Split("/");
                                                var term = yrtrParts[0];
                                                Year = yrtrParts[1];
                                                Yrtr = term + Year;
                                            }

                                            if (section.status != null)
                                            {
                                                status = section.status.category;
                                            }

                                            if (section.credits != null)
                                            {
                                                creditType = section.credits[0].creditCategory.detail.id;
                                            }
                                            break;
                                        }
                                    }
                                    _logger.LogString($"index: {i}, couId: {couId}", LogEntryType.Info);
                                    //if (Year != "AY" && status == "open" && creditType == "institution")
                                    if (Year != "AY" && (creditType == CRE || creditType == DEV) && !isCancelled)
                                    {
                                        foreach (var facId in facIds)
                                        {
                                            _logger.LogString($"facId: {facId}", LogEntryType.Info);

                                            var persons = await personService.GetPersonByColleagueIdAsync(facId);
                                            var guidId = "";
                                            if (persons != null)
                                            {
                                                foreach (var guid in persons)
                                                {
                                                    guidId = guid.id;
                                                }

                                            }
                                            //to build csv of techIds
                                            string line = $"{guidId},{facId}";

                                            // Write the record line  
                                            //COU_ID,YRTR,TECH_ID
                                            string recordLine = $"{couId},{Yrtr},{facId}";
                                            if (facValues.Add(recordLine))
                                            {
                                                await writer.WriteLineAsync(recordLine);
                                                Console.WriteLine(recordLine);

                                                if (uniqueLines.Add(line)) // Add returns false if already exists
                                                {
                                                    //techids.Add(new TechIds { guid = id, colleagueId = techId });
                                                    writer2.WriteLine(line);
                                                    writer2.Flush();
                                                }
                                            _logger.LogString($"recordLine: {recordLine}:", LogEntryType.Info);
                                                _logger.LogString($"Index, secId, year, status, creditType: {i},{sectionId},{Yrtr},{status},{creditType}:", LogEntryType.Info);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}

