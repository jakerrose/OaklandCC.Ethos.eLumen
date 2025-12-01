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
using static System.Collections.Specialized.BitVector32;
using System.Text.Json;
using Newtonsoft.Json;

namespace OaklandCC.Ethos.eLumen.Services
{
    public class EnrollmentService
    {
        private readonly EthosClient _ethosClient;
        private readonly IConfiguration _configuration;
        private readonly Logger _logger;


        /// <summary>  
        /// Initializes a new instance of the <see cref="Enrollment"/> class.  
        /// </summary>  
        /// <param name="ethosClient">The Ethos client for API interactions.</param>  
        /// <param name="configuration">The application configuration settings.</param>  
        /// <param name="logger">The logger for logging messages.</param>  
        public EnrollmentService(EthosClient ethosClient, IConfiguration configuration, Logger logger)
        {
            _ethosClient = ethosClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task CreateEnrollmentFile()
        {
            //To ensure no duplicate rows
            HashSet<string> enrollmentValues = new HashSet<string>();
            
            //var techids = new List<TechIds>();
            var uniqueLines = new HashSet<string>();
            var service = new InitiationService();

            //******************To write ENROLLMENT.csv

            var academicPeriodId = UserInput.StoredAcadPeriod;
            var TermCode = UserInput.StoredAcadCode;
            var newTermCode = TermCode.Replace("/", "");

            string baseFileName = _configuration["FileSettings:EnrollmentOutputFilePath"] ?? "ENROLLMENT.csv";
            string fileNameWithTerm = Path.GetFileNameWithoutExtension(baseFileName) + $"_{newTermCode}" + Path.GetExtension(baseFileName);
            //get start on date
            var acPerService = new AcademicPeriodService2(_ethosClient, _logger);
            AcademicPeriod2 acadObject = await acPerService.GetAcademicPeriodByIdAsync(academicPeriodId);

            //other services
            var secRegService = new SectionRegistrationService(_ethosClient);
            //var secIdService = new CourseSectionService(_ethosClient);
            var personService = new PersonService(_ethosClient);
            //var sectionService = new CourseSectionService(_ethosClient);
            var gradeService = new GradeService(_ethosClient);

            //using json file
            List<SectionById> sections = new List<SectionById>();
            var json = File.ReadAllText("./sections.json");
            sections = JsonConvert.DeserializeObject<List<SectionById>>(json);
            Console.WriteLine("Sections loaded: " + sections.Count);

            List<GradeDefinitions> gradeDefs = new List<GradeDefinitions>();
            var json2 = File.ReadAllText("./gradeDefs.json");
            gradeDefs = JsonConvert.DeserializeObject<List<GradeDefinitions>>(json2);
            Console.WriteLine("Grade definitions loaded " + gradeDefs.Count);

            List<SectionRegistration> secRegList = new List<SectionRegistration>();
            var json6 = File.ReadAllText("./sectionRegistrations.json");
            secRegList = JsonConvert.DeserializeObject<List<SectionRegistration>>(json6);

            List<Person> students = new List<Person>();
            var json3 = File.ReadAllText("./persons.json");
            students = JsonConvert.DeserializeObject<List<Person>>(json3);

            //List<SectionRegistration> secRegList = await secRegService.GetAllSectionRegistrationsByAcademicPeriodIdAsync(academicPeriodId);

            using (StreamWriter writer = new StreamWriter(fileNameWithTerm, append: false))
            {
                // Write header line  
                await writer.WriteLineAsync("COU_ID,YRTR,TECH_ID,DROP_TIME_STAMP");

                string filePath2 = "techids.csv";
                // Optional: add header line
                using (var writer2 = new StreamWriter(filePath2, append: true))
                {
                    // Process each location  
                    for (int i =0; i < secRegList.Count; i++)
                {
                    Console.WriteLine("index: " + i);
                    var secReg = secRegList[i];
                    var secRegId = secReg.id; 
                    var id = secReg.registrant.id;
                    
                    Console.WriteLine("secReg:" + secRegId);
                    Console.WriteLine("id: " + id);
                    _logger.LogString($"index: {i}, regId: {id}", LogEntryType.Info);

                    //section registration statuses
                    var N = "d016f524-b63e-4821-801d-82bea77d777d"; 
                    var A = "581cae92-99e0-4ba8-a391-f329ddcdfc1a";
                    var D = "724aba8e-cfce-4234-87eb-9ce5c13ac6aa";
                    var W = "1ebd4fff-8171-412f-878c-0c0e03dd3bc2";
                    var X = "9d25bb16-cca9-4516-89a0-34d271723e46";
                    var C = "3284bf83-0297-4616-8c55-c5215c2bf24f";
                    var DR = "7a77a59e-990b-429b-a61a-a07e3233c737";

                    //credit categories
                    var CRE = "68ac1a07-440a-4830-99e6-a19b4263f3a8"; 
                    var DEV = "f1d869b2-8920-471b-8c6c-b4b1fab05283";

                    var secRegStatus = secReg.status?.detail?.id;

                    _logger.LogString($"index, secRegid, secRegStatus: {i}, {id}, {secRegStatus}", LogEntryType.Info);
                    if (secRegStatus == null) continue;

                    if (secRegStatus == N || secRegStatus == A || secRegStatus == D || secRegStatus == W)
                    {

                        //to get DROP_TIME_STAMP
                        var droppedDate = "";
                        if (secRegStatus == D || secRegStatus == W)
                        {
                            droppedDate = secReg.statusDate.Replace("-", "");
                            _logger.LogString($"droppedDate: {droppedDate}", LogEntryType.Info);

                        }
                        //to get course Id and term code
                        var sectionId = secReg.section.id;

                        // to get TECH_ID
                        var techId = "";
                        bool techIdFound = false;

                            //Person students = await personService.GetPersonByIdAsync(id);
                            
                            foreach (var student in students)
                            { 
                                if (student.id == id)
                                {
                                    if (student.credentials != null)
                                    {
                                        foreach (var credential in student.credentials)
                                        {
                                            switch (credential.type)
                                            {
                                                case "colleaguePersonId":
                                                    techId = credential.value;
                                                    _logger.LogString($"techId: {techId}", LogEntryType.Info);
                                                    techIdFound = true;
                                                    break;
                                            }
                                        }
                                    }                               
                                }
                                if (techIdFound) { break; }
                            }
                      
                        var courseSecId = "";
                        var termCode = "";
                        var termSeason = "";
                        var creditType = "";
                        var Instid = "";
                        var cancelled = "e8ab394d-b5bf-42bd-834a-3e973664eb48";
                        bool isCancelled = false;


                        foreach (var section in sections)
                        {
                            if (sectionId == section.id)
                            {
                                if (section.credits != null && section.credits.Count > 0 &&
                                    section.credits[0].creditCategory?.detail?.id != null)
                                {
                                    creditType = section.credits[0].creditCategory.detail.id;
                                    _logger.LogString($"credityType: {creditType}", LogEntryType.Info);
                                }
                                else
                                {
                                    continue; // or set to "unknown"
                                }
                                courseSecId = section.courseSectionsId;
                                termCode = section.termCode;
                                string[] termcodeParts = termCode.Split('/');
                                var termYear = termcodeParts[0];
                                termSeason = termcodeParts[1];
                                termCode = termYear + termSeason;

                                Instid = section.owningInstitutionUnits[0].institutionUnit.id;
                                if (section.instructionalMethods != null)
                                {
                                    foreach (var method in section.instructionalMethods)
                                    {
                                        _logger.LogString($"methodId: {method.id}", LogEntryType.Info);
                                        if (method.id == cancelled)
                                        {
                                            isCancelled = true;
                                        }

                                    }
                                }
                                break;
                            }
                        }
                        //To check final grade
                        bool found = false;
                        List<StudentUnverifiedGrades> stcs = await gradeService.GetUGradesByStduent(id);
                        var gradeValue = "";
                            foreach(var stc in stcs)
                            {
                                if(secRegId == stc.sectionRegistration.id)
                                {
                                    if (stc.details.grades != null)
                                    {
                                        gradeValue = stc.details.grades[0].grade.id;
                                    }
                                    else
                                    {
                                        gradeValue = "Not found";
                                    }
                                }
                            }
                            var gradeN = "fbdb38f9-9fe2-45ce-9e6f-8290894e431c";
                            var gradeNC = "b7366c60-6403-4f98-aa03-83330a05e933";
                            var gradeNP = "36a8e48f-fa69-41d3-b87e-c31edd8dd6d0";

                            //to build csv of techIds
                            string line = $"{id},{techId}";

                        //COU_ID,YRTR,TECH_ID,DROP_TIME_STAMP
                        string recordLine = $"{courseSecId},{termCode},{techId},{droppedDate}";
                        Console.WriteLine(recordLine);
                        _logger.LogString($"recordLine: {recordLine}, secReg: {secRegId}", LogEntryType.Info);
                            if (droppedDate.Length > 0)
                            {
                                if (enrollmentValues.Add(recordLine) && termSeason != "AY" &&
                            (creditType == CRE || creditType == DEV) && !isCancelled)
                                {
                                    await writer.WriteLineAsync(recordLine);
                                    _logger.LogString($"wrote line: {recordLine}", LogEntryType.Info);
                                    if (uniqueLines.Add(line)) // Add returns false if already exists
                                    {
                                        //techids.Add(new TechIds { guid = id, colleagueId = techId });
                                        writer2.WriteLine(line);
                                    }
                                }
                            }
                            else
                            {
                                if (enrollmentValues.Add(recordLine) && termSeason != "AY" &&
                                   (creditType == CRE || creditType == DEV) && !isCancelled && gradeValue != gradeN
                                   && gradeValue != gradeNC && gradeValue != gradeNP)
                                {
                                    await writer.WriteLineAsync(recordLine);
                                    _logger.LogString($"wrote line: {recordLine}", LogEntryType.Info);
                                    if (uniqueLines.Add(line)) // Add returns false if already exists
                                    {
                                        //techids.Add(new TechIds { guid = id, colleagueId = techId });
                                        writer2.WriteLine(line);
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

