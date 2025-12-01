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
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using Newtonsoft.Json;
using static System.Collections.Specialized.BitVector32;

namespace OaklandCC.Ethos.eLumen.Services
{
    public class OfferingSecattrService
    {
        private readonly EthosClient _ethosClient;
        private readonly IConfiguration _configuration;
        private readonly Logger _logger;

        /// <summary>  
        /// Initializes a new instance of the <see cref="OfferingSecattrService"/> class.  
        /// </summary>  
        /// <param name="ethosClient">The Ethos client for API interactions.</param>  
        /// <param name="configuration">The application configuration settings.</param>  
        /// <param name="logger">The logger for logging messages.</param>  
        public OfferingSecattrService(EthosClient ethosClient, IConfiguration configuration, Logger logger)
        {
            _ethosClient = ethosClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task CreateOfferingSecattrFile()
        {

            //to write OFFERING.csv
            //var sectionService = new CourseSectionService(_ethosClient);
            var secRegService = new SectionRegistrationService(_ethosClient);
            var acadService = new AcademicPeriodService2(_ethosClient, _logger);
            //var eduService = new EducationalInstitutionUnitService(_ethosClient);
            //var courseService = new CourseSectionService(_ethosClient);
            var miscService = new MiscService(_ethosClient);
            var methodService = new InstructionalMethodService(_ethosClient);
            var siteService = new SitesService(_ethosClient);
            var courseModes = new List<CourseMode>();
            var eventsService = new InstructionalEventService(_ethosClient);

            List<MiscText> elementList = await miscService.GetAllTextAsync(limit: 500);
            foreach (var element in elementList)
                if (element.id == "misc.text-2belumen_instr_method")
                {
                    {
                        var input2 = element.text;
                        string pattern = @"([A-Z0-9/-]{2,10})\|([^\|]+?)(?=\s+[A-Z0-9/-]{2,10}\||$)";

                        var matches = Regex.Matches(input2, pattern);


                        foreach (Match match in matches)
                        {
                            var mode = new CourseMode
                            {
                                Code = match.Groups[1].Value.Trim(),
                                Description = match.Groups[2].Value.Trim()
                            };

                            courseModes.Add(mode);
                        }
                    }
                }

            
            //using json file
            List<SectionById> sections = new List<SectionById>();
            var json = File.ReadAllText("./sections.json");
            sections = JsonConvert.DeserializeObject<List<SectionById>>(json);

            List<Site> sites = new List<Site>();
            var json2 = File.ReadAllText("./sites.json");
            sites = JsonConvert.DeserializeObject<List<Site>>(json2);

            List<InstructionalEvent> events = new List<InstructionalEvent>();
            var json4 = File.ReadAllText("./events.json");
            events = JsonConvert.DeserializeObject<List<InstructionalEvent>>(json4);

            List<InstructionalMethods> methods = new List<InstructionalMethods>();
            var json3 = File.ReadAllText("./methods.json");
            methods = JsonConvert.DeserializeObject<List<InstructionalMethods>>(json3);

            List<InstructionalEventById> eventList = new List<InstructionalEventById>();
            var json5 = File.ReadAllText("./eventSections.json");
            eventList = JsonConvert.DeserializeObject<List<InstructionalEventById>>(json5);

            List<SectionRegistration> secRegList = new List<SectionRegistration>();
            var json6 = File.ReadAllText("./sectionRegistrations.json");
            secRegList = JsonConvert.DeserializeObject<List<SectionRegistration>>(json6);

            //to ensure no duplicates
            HashSet<string> offeringValues = new HashSet<string>();

            var academicPeriodId = UserInput.StoredAcadPeriod;
            var termCode = UserInput.StoredAcadCode;
            var newTermCode = termCode.Replace("/", "");

            string baseFileName = _configuration["FileSettings:OfferingSecattrOutputFilePath"] ?? "OFFERINGSECATTR.csv";
            string fileNameWithTerm = Path.GetFileNameWithoutExtension(baseFileName) + $"_{newTermCode}" + Path.GetExtension(baseFileName);
            
            //string? filePath = _configuration["FileSettings:OfferingSecattrOutputFilePath"];
            //if (string.IsNullOrEmpty(filePath))
            //{
            //    _logger.LogString("Error: File path is not configured in appsettings.json.", LogEntryType.Error);
            //    return;
            //}
            //_logger.LogString($"OfferingSecattrOutputFilePath: {filePath}:", LogEntryType.Info);

            //academic period drop date
            AcademicPeriod2 academicPeriod = await acadService.GetAcademicPeriodByIdAsync(academicPeriodId);
            var termDropStart = academicPeriod.termDropStartDate.Replace("-", "");
            int dropStart = Int32.Parse(termDropStart);

            //List<Section> sectionList = await sectionService.GetAllSectionsPagedAsync(periodId, pageSize: 100);


            using (StreamWriter writer = new StreamWriter(fileNameWithTerm, append: false))
            {
                // Write header line  
                await writer.WriteLineAsync("COU_ID,YRTR,ID");
                // loop through each section registration
                //List<SectionRegistration> secRegList = await secRegService.GetAllSectionRegistrationsByAcademicPeriodIdAsync(academicPeriodId);

                for (int i = 0; i < secRegList.Count; i++)
                {
                    var secReg = secRegList[i];
                    Console.WriteLine("index: " + i);
                    _logger.LogString($"index: {i}", LogEntryType.Info);
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
                    //check secReg status
                    bool badStatus = false;
                    if (secRegStausDetail == X || secRegStausDetail == C || secRegStausDetail == DR)
                    {
                        badStatus = true;
                    }
                    if (!badStatus)
                    {
                        if (secRegStausDetail == X || secRegStausDetail == W || secRegStausDetail == D || secRegStausDetail == DR || secRegStausDetail == C)
                        {
                            int droppedDateNum = Int32.Parse(droppedDate);
                            if (droppedDateNum < dropStart)
                            {
                                //bypass
                                dropEarly = true;
                            }
                        }

                        if (!dropEarly)
                        {

                            var sectionId = secReg.section.id;
                            _logger.LogString($"sectionId: {sectionId}", LogEntryType.Info);
                            var techId = "";
                            var yrtr = "";
                            var courseNo = "";
                            var id = "";

                            //SectionById sectionList = await sectionService.GetSectionsByIdAsync(sectionId);

                            var cancelled = "e8ab394d-b5bf-42bd-834a-3e973664eb48";
                            var cancelled2 = "d03d13df-eee5-44a7-bc8b-047cb845111f";

                            //credit categories
                            var CRE = "68ac1a07-440a-4830-99e6-a19b4263f3a8";
                            var DEV = "f1d869b2-8920-471b-8c6c-b4b1fab05283";

                            bool iscancelled = false;
                            var creditType = "";

                            HashSet<string> instMethods = new HashSet<string>();

                            foreach (var sectionList in sections)
                            {
                                if (sectionId == sectionList.id)
                                {
                                    courseNo = sectionList.courseSectionsId;
                                    yrtr = sectionList.termCode.Replace("/", "");
                                    if (sectionList.credits != null &&
                                    sectionList.credits[0].creditCategory?.detail?.id != null)
                                    {
                                        creditType = sectionList.credits[0].creditCategory.detail.id;
                                        _logger.LogString($"creditType: {creditType}", LogEntryType.Info);
                                    }

                                    //check instructional methods for cancelled

                                    if (sectionList.instructionalMethods != null)
                                    {
                                        foreach (var method in sectionList.instructionalMethods)
                                        {
                                            instMethods.Add(method.id);

                                        }
                                    }

                                    foreach (var methodId in instMethods)
                                    {
                                        Console.WriteLine("methodId: " + methodId);
                                        if (methodId == cancelled || methodId == cancelled2)
                                        {
                                            iscancelled = true;
                                        }
                                    }
                                    if (sectionList.site != null)
                                    {
                                        id = sectionList.site.id;
                                    }
                                        break;
                                }
                            }
                            //-------for TechId is Site
                            if (id != null)
                            {
                                foreach (var site in sites)
                                {
                                    if (id == site.id)
                                    {
                                        var campusId = site.code;

                                        var firstLine = $"{courseNo},{yrtr},{campusId}";
                                        if (offeringValues.Add(firstLine) && campusId != "OF" && !iscancelled 
                                            && (creditType == CRE || creditType == DEV))
                                        {
                                            await writer.WriteLineAsync(firstLine);
                                            Console.WriteLine($"campusLine: {firstLine}");
                                            _logger.LogString($"campusLine: {firstLine}: index:+{i}", LogEntryType.Info);
                                        }
                                        break;
                                    }
                                }
                            }

                            //--------------------------for Tech Id is meeting time
                            var iMethodId = "";
                            var finalTime = "";
                            var secondLine = "";
                            //List<InstructionalEventById> eventList = await eventsService.GetInstructionalEventsBySectionIdAsync(sectionId);
                            bool meetFound = false; 
                            for (int k = 0; k < eventList.Count; k++)
                            {
                                var iEvent = eventList[k];
                                if (iEvent.section.id == sectionId)
                                {
                                    iMethodId = iEvent.instructionalMethod?.id;
                                    bool weekday = false;
                                    if ((iEvent.recurrence?.repeatRule?.daysOfWeek?.Any(d => d == "monday" || d == "tuesday" || d == "wednesday"
                                    || d == "thursday" || d == "friday") == true) || iEvent.recurrence?.repeatRule?.type == "daily")
                                    {
                                        weekday = true;
                                        var timeText = iEvent.startTime;
                                        string[] timeParts = timeText.Split(":");
                                        timeText = $"{timeParts[0]}{timeParts[1]}";
                                        int timeStartInt = Int32.Parse(timeText);
                                        if (timeStartInt <= 1159)
                                        {
                                            finalTime = "Morning";
                                            meetFound = true;
                                        }
                                        else if (timeStartInt > 1159 && timeStartInt < 1700)
                                        {
                                            finalTime = "Afternoon";
                                            meetFound = true;
                                        }
                                        else if (timeStartInt >= 1700)
                                        {
                                            finalTime = "Evening";
                                            meetFound = true;
                                        }

                                    }
                                    if (!weekday)
                                    {
                                        if (iEvent.recurrence?.repeatRule?.daysOfWeek?.Any(d => d == "saturday" || d == "sunday") == true)
                                        {
                                            finalTime = "Weekend";
                                            meetFound = true;

                                        }
                                        else
                                        {
                                            finalTime = "Not Applicable";
                                            meetFound = true;
                                        }
                                    }

                                    break;
                                }
                            }
                            if(!meetFound)
                            {
                                finalTime = "Not Applicable";
                            }
                            //-------for TechId is Instructional Method
                            //code is this abbrviation e.g. AUG
                            bool codeFound = false;

                            foreach (var methodList in methods)
                            {
                                if (iMethodId == methodList.id)
                                {
                                    var code = methodList.abbreviation;
                                    //uses values previously saved in class CourseMode, loops through all to find match
                                    foreach (var mode in courseModes)
                                    {
                                        if (code == mode.Code)
                                        {
                                            techId = mode.Description;
                                            codeFound = true;

                                            var thirdLine = $"{courseNo},{yrtr},{techId}";

                                            if (offeringValues.Add(thirdLine) && !iscancelled && (creditType == CRE || creditType == DEV))
                                            {
                                                await writer.WriteLineAsync(thirdLine);
                                                Console.WriteLine($"instructionalMethodLine: {thirdLine}");
                                                _logger.LogString($"instructionalMethodLine: {thirdLine}: index:+{i}", LogEntryType.Info);
                                                
                                                goto loopEnd;
                                                //break;
                                            }
                                        }
                                    }
                                }
                            }
                        loopEnd:;
                            if (!codeFound)
                            {
                                techId = "On-Ground";
                                var thirdLine = $"{courseNo},{yrtr},{techId}";

                                if (offeringValues.Add(thirdLine) && !iscancelled && (creditType == CRE || creditType == DEV))
                                {
                                    await writer.WriteLineAsync(thirdLine);
                                    Console.WriteLine($"instructionalMethodLine: {thirdLine}");
                                    _logger.LogString($"instructionalMethodLine: {thirdLine}: index:+{i}", LogEntryType.Info);
                                }
                            }

                            //Write meeting time if TechId is not Online
                            secondLine = $"{courseNo},{yrtr},{finalTime}";
                            if (offeringValues.Add(secondLine) && !iscancelled && !techId.StartsWith("Onl")
                                && (creditType == CRE || creditType == DEV))
                            {
                                await writer.WriteLineAsync(secondLine);
                                Console.WriteLine($"meetingTimeLine: {secondLine}");
                                _logger.LogString($"meetingTimeLine: {secondLine}: index:+{i}", LogEntryType.Info);
                            }

                        }
                    }                        
                }
            }
        }
    }
}