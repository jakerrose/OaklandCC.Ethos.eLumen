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
using System.Text.RegularExpressions;
using static System.Runtime.CompilerServices.RuntimeHelpers;
using static System.Net.Mime.MediaTypeNames;
using static System.Collections.Specialized.BitVector32;

namespace OaklandCC.Ethos.eLumen.Services
{
    public class Combine4Services
    {
        private readonly EthosClient _ethosClient;
        private readonly IConfiguration _configuration;
        private readonly Logger _logger;
        /// <summary>  
        /// Initializes a new instance of the <see cref="Combine4Services"/> class.  
        /// </summary>  
        /// <param name="ethosClient">The Ethos client for API interactions.</param>  
        /// <param name="configuration">The application configuration settings.</param>  
        /// <param name="logger">The logger for logging messages.</param>  
        public Combine4Services(EthosClient ethosClient, IConfiguration configuration, Logger logger)
        {
            _ethosClient = ethosClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task Create4Files()
        {

            //to write OFFERING.csv,OFFERINGSECATTR.csv,FACULTYASSIGNMENT.csv, and ENROLLMENT.csv

            var service = new InitiationService();
            var acadService = new AcademicPeriodService2(_ethosClient, _logger);
            var facultyService = new FacultyService(_ethosClient);
            var gradeService = new GradeService(_ethosClient);
            var miscService = new MiscService(_ethosClient);
            var personService = new PersonService(_ethosClient);
            var courseModes = new List<CourseMode>();
            var techids = new List<TechIds>();
            HashSet<string> instMethods = new HashSet<string>();

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
            Console.WriteLine("Sections loaded: " + sections.Count);

            List<SectionRegistration> secRegList = new List<SectionRegistration>();
            var json8 = File.ReadAllText("./sectionRegistrations.json");
            secRegList = JsonConvert.DeserializeObject<List<SectionRegistration>>(json8);

            List<GradeDefinitions> gradeDefs = new List<GradeDefinitions>();
            var json7 = File.ReadAllText("./gradeDefs.json");
            gradeDefs = JsonConvert.DeserializeObject<List<GradeDefinitions>>(json7);
            Console.WriteLine("Grade definitions loaded " + gradeDefs.Count);

            List<EducationalInstitutionUnit> eduList = new List<EducationalInstitutionUnit>();
            var json2 = File.ReadAllText("./units.json");
            eduList = JsonConvert.DeserializeObject<List<EducationalInstitutionUnit>>(json2);
            Console.WriteLine("Units loaded: " + eduList.Count);

            List<CourseById> courseList = new List<CourseById>();
            var json3 = File.ReadAllText("./courses.json");
            courseList = JsonConvert.DeserializeObject<List<CourseById>>(json3);
            Console.WriteLine("Courses loaded: " + courseList.Count);

            List<InstructionalEvent> events = new List<InstructionalEvent>();
            var json4 = File.ReadAllText("./events.json");
            events = JsonConvert.DeserializeObject<List<InstructionalEvent>>(json4);
            Console.WriteLine("Events loaded " + events.Count);

            List<Site> sites = new List<Site>();
            var json5 = File.ReadAllText("./sites.json");
            sites = JsonConvert.DeserializeObject<List<Site>>(json5);
            Console.WriteLine("Sites loaded " + sites.Count);

            List<InstructionalMethods> methods = new List<InstructionalMethods>();
            var json6 = File.ReadAllText("./methods.json");
            methods = JsonConvert.DeserializeObject<List<InstructionalMethods>>(json6);
            Console.WriteLine("Instructional methods loaded " + methods.Count);

            List<InstructionalEventById> eventList = new List<InstructionalEventById>();
            var json10 = File.ReadAllText("./eventSections.json");
            eventList = JsonConvert.DeserializeObject<List<InstructionalEventById>>(json10);
            Console.WriteLine("Event sections loaded " + eventList.Count);

            List<Person> students = new List<Person>();
            var json11 = File.ReadAllText("./persons.json");
            students = JsonConvert.DeserializeObject<List<Person>>(json11);
            Console.WriteLine("Persons loaded " + students.Count);

            //to ensure no duplicates
            HashSet<string> offeringValues = new HashSet<string>();
            HashSet<string> offeringSecAttrValues = new HashSet<string>();
            HashSet<string> facValues = new HashSet<string>();
            HashSet<string> enrollmentValues = new HashSet<string>();
            var uniqueLines = new HashSet<string>();

            var academicPeriodId = UserInput.StoredAcadPeriod;
            var termCode = UserInput.StoredAcadCode;
            var newTermCode = termCode.Replace("/", "");

            // Retrieve file path from appsettings.json
            // For Offering
            string baseFileName = _configuration["FileSettings:OfferingOutputFilePath"] ?? "OFFERING.csv";
            string filePath = Path.GetFileNameWithoutExtension(baseFileName) + $"_{newTermCode}" + Path.GetExtension(baseFileName);

            //for OfferingSecattr 
            string baseFileName2 = _configuration["FileSettings:OfferingSecattrOutputFilePath"] ?? "OFFERINGSECATTR.csv";
            string filePath2 = Path.GetFileNameWithoutExtension(baseFileName2) + $"_{newTermCode}" + Path.GetExtension(baseFileName2);

            //for FacultyAssignment
            string baseFileName3 = _configuration["FileSettings:FacAssOutputFilePath"] ?? "FACULTYASSIGNMENT.csv";
            string filePath3 = Path.GetFileNameWithoutExtension(baseFileName3) + $"_{newTermCode}" + Path.GetExtension(baseFileName3);

            //for Enrollment
            string baseFileName4 = _configuration["FileSettings:EnrollmentOutputFilePath"] ?? "ENROLLMENT.csv";
            string filePath4 = Path.GetFileNameWithoutExtension(baseFileName4) + $"_{newTermCode}" + Path.GetExtension(baseFileName4);

            //academic period drop date
            AcademicPeriod2 academicPeriod = await acadService.GetAcademicPeriodByIdAsync(academicPeriodId);
            var termDropStart = academicPeriod.termDropStartDate.Replace("-", "");
            int dropStart = Int32.Parse(termDropStart);

            // Write header line 1 for Offering
            using (StreamWriter writer = new StreamWriter(filePath, append: true))
            {
                await writer.WriteLineAsync("CURRIC_ID,SUBJ,COU_NBR,TITLE,DS,ORG_OWNER_ID,COU_ID,YRTR,SECT_NBR, COU_TYPE,CAMPUSID,DELETE_SECTION");

                // Write header line 2 for OfferingSecattr
                using (StreamWriter writer2 = new StreamWriter(filePath2, append: true))
                {
                    await writer2.WriteLineAsync("COU_ID,YRTR,TECH_ID");

                    // Write header line 3 for FacultyAssignment
                    using (StreamWriter writer3 = new StreamWriter(filePath3, append: true))
                    {
                        await writer3.WriteLineAsync("COU_ID,YRTR,TECH_ID");

                        // Write header line 4 Enrollment
                        using (StreamWriter writer4 = new StreamWriter(filePath4, append: true))
                        {
                            await writer4.WriteLineAsync("COU_ID,YRTR,TECH_ID,DROP_TIME_STAMP");

                            //To add to list of person Ids
                            string filePath5 = "techids.csv";

                            // Optional: add header line
                            using (var writer5 = new StreamWriter(filePath5, append: true))
                            {

                                Console.WriteLine("Starting loop for writing OFFERING.csv,OFFERINGSECATTR.csv,FACULTYASSIGNMENT.csv, and ENROLLMENT.csv");
                                //loop through section registrations file
                                for (int i = 0; i < secRegList.Count; i++)
                                {
                                    var secReg = secRegList[i];
                                    Console.WriteLine("Processing loop " + i + " of " + secRegList.Count);
                                    _logger.LogString($"index: {i}", LogEntryType.Info);
                                    var id = secReg.registrant.id;
                                    var secRegId = secReg.id;

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
                                        //to get DROP_TIME_STAMP
                                        var droppedDate = "";
                                        var dropStatus = secReg.status.sectionRegistrationStatusReason;
                                        if (dropStatus == "dropped" || dropStatus == "withdrawn")
                                        {
                                            droppedDate = secReg.statusDate.Replace("-", "");

                                        }

                                        if (secRegStausDetail == W || secRegStausDetail == D)
                                        {
                                            int droppedDateNum = Int32.Parse(droppedDate);
                                            if (droppedDateNum < dropStart)
                                            {
                                                dropEarly = true;
                                            }
                                        }

                                        var sectionId = secReg.section.id;
                                        var meetingText = "";
                                        var CRE = "68ac1a07-440a-4830-99e6-a19b4263f3a8";
                                        var DEV = "f1d869b2-8920-471b-8c6c-b4b1fab05283";
                                        var cancelled = "e8ab394d-b5bf-42bd-834a-3e973664eb48";
                                        var cancelled2 = "d03d13df-eee5-44a7-bc8b-047cb845111f";
                                        bool isCancelled = false;
                                        var courseId = "";
                                        var creditType = "";
                                        var couId = "";
                                        var sectNumber = "";
                                        var subject = "";
                                        var courseNo = "";
                                        var yrtr = "";
                                        var owningId = "";
                                        var siteId = "";
                                        var season = "";
                                        var Year = "";
                                        var techId = "";

                                        foreach (var section in sections)
                                        {
                                            if (secReg.section.id == section.id)
                                            {
                                                courseId = section.course.id;

                                                if (section.credits != null)
                                                {
                                                    creditType = section.credits[0].creditCategory.detail.id;

                                                }
                                                if (section.site != null)
                                                {
                                                    siteId = section.id;
                                                }
                                                meetingText = section.meetingTimes;
                                                couId = section.courseSectionsId;
                                                sectNumber = section.number;
                                                subject = section.code.Split("-")[0];
                                                courseNo = section.code.Split("-")[1];
                                                season = section.termCode.Split("/")[1];
                                                Year = section.termCode.Split('/')[0];
                                                yrtr = section.termCode.Replace("/", "");
                                                owningId = section.owningInstitutionUnits[0].institutionUnit.id;
                                                //check if cancelled
                                                if (section.instructionalMethods != null)
                                                {
                                                    foreach (var method in section.instructionalMethods)
                                                    {
                                                        if (method.id == cancelled)
                                                        {
                                                            isCancelled = true;
                                                        }
                                                    }
                                                }
                                                break;
                                            }
                                        }

                                        //to get org owner id
                                        var orgOwnerId = "";
                                        foreach (var edus in eduList)
                                        {
                                            if (owningId == edus.id)
                                            {
                                                orgOwnerId = edus.code;
                                                break;
                                            }
                                        }

                                        //to get description, title, and ciric id                                
                                        var ciricID = "";
                                        var desc = "";
                                        var title = "";

                                        foreach (var course in courseList)
                                        {
                                            if (courseId == course.id)
                                            {

                                                ciricID = course.coursesId;

                                                desc = SanitizeFilename2(course.description);
                                                int maxLength = 2000;
                                                // Truncate the filename if it exceeds the maximum length
                                                if (desc.Length > maxLength)
                                                {
                                                    desc = desc.Substring(0, maxLength);
                                                }
                                                title = SanitizeFilename(course.titles[1].value);
                                            }
                                        }

                                        var couType = "";
                                        var campId = "";
                                        var delete = "";


                                        //----------------------------OFFERING!----------------------------------------------??
                                        //// Write the record line  
                                        //CURRIC_ID,SUBJ,COU_NBR,TITLE,DS,ORG_OWNER_ID,COU_ID,YRTR,SECT_NBR, COU_TYPE,CAMPUSID,DELETE_SECTION
                                        string recordLine = $"{ciricID.Trim()},{subject.Trim()},{courseNo.Trim()},{title.Trim()},{desc.Trim()},{orgOwnerId.Trim()},{couId.Trim()},{yrtr.Trim()}," +
                                            $"{sectNumber},{couType},{campId},{delete}";
                                        if (offeringValues.Add(recordLine) && (creditType == CRE || creditType == DEV) && !dropEarly && !isCancelled)
                                        {
                                            await writer.WriteLineAsync(recordLine);
                                            //_logger.LogString($"recordLine: {recordLine}: index: {i}", LogEntryType.Info);
                                            Console.Write("Wrote record to Offering: "+recordLine);
                                        }



                                        //-------------------------------------------OFFERINGSECATTR.CSV--------------------------------------------------------//


                                        //-------for TechId is Site
                                        if (siteId != null)
                                        {
                                            foreach (var site in sites)
                                            {
                                                if (id == site.id)
                                                {
                                                    var campusId = site.code;

                                                    var firstLine = $"{couId},{yrtr},{campusId}";
                                                    if (offeringSecAttrValues.Add(firstLine) && campusId != "OF" && !isCancelled
                                                        && (creditType == CRE || creditType == DEV) && !dropEarly)
                                                    {
                                                        await writer2.WriteLineAsync(firstLine);
                                                        Console.WriteLine($"Wrote recrod to OfferingSecattr: campusLine: {firstLine}");
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
                                        if (!meetFound)
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

                                                        if (offeringSecAttrValues.Add(thirdLine) && !isCancelled && (creditType == CRE || creditType == DEV) && !dropEarly)
                                                        {
                                                            await writer2.WriteLineAsync(thirdLine);
                                                            Console.WriteLine($"Wrote record to OfferingSecattr: instructionalMethodLine: {thirdLine}");
                                                            _logger.LogString($"instructionalMethodLine: {thirdLine}: index:+{i}", LogEntryType.Info);

                                                            goto loopEnd;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    loopEnd:;
                                        if (!codeFound)
                                        {   //default is On-Ground if not found
                                            techId = "On-Ground";
                                            var thirdLine = $"{courseNo},{yrtr},{techId}";

                                            if (offeringSecAttrValues.Add(thirdLine) && !isCancelled && (creditType == CRE || creditType == DEV) && !dropEarly)
                                            {
                                                await writer2.WriteLineAsync(thirdLine);
                                                Console.WriteLine($"Wrote record to OfferingSecattr: instructionalMethodLine: {thirdLine}");
                                                _logger.LogString($"instructionalMethodLine: {thirdLine}: index:+{i}", LogEntryType.Info);
                                            }
                                        }

                                        //Write meeting time if TechId is not Online
                                        secondLine = $"{courseNo},{yrtr},{finalTime}";
                                        if (offeringSecAttrValues.Add(secondLine) && !isCancelled && !techId.StartsWith("Onl")
                                            && (creditType == CRE || creditType == DEV) && !dropEarly)
                                        {
                                            await writer2.WriteLineAsync(secondLine);
                                            Console.WriteLine($"Wrote record to OfferingSecattr: meetingTimeLine: {secondLine}");
                                            _logger.LogString($"meetingTimeLine: {secondLine}: index:+{i}", LogEntryType.Info);
                                        }

                                        //--------------------------------FACULTY ASSIGNMENT!
                                        List<Faculty> facultyList = await facultyService.GetInstructors(sectionId: $"{sectionId}", limit: 100);
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
                                            if (Year != "AY" && (creditType == CRE || creditType == DEV)
                                                 && !dropEarly && !isCancelled)
                                            {
                                                foreach (var facId in facIds)
                                                {

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
                                                    string recordLine2 = $"{couId},{yrtr},{facId}";
                                                    if (facValues.Add(recordLine2))
                                                    {
                                                        await writer3.WriteLineAsync(recordLine2);
                                                        Console.WriteLine("Wrote record to FacultyAssignment "+recordLine2);

                                                        if (uniqueLines.Add(line)) // Add returns false if already exists
                                                        {
                                                            //techids.Add(new TechIds { guid = id, colleagueId = techId });
                                                            writer5.WriteLine(line);
                                                            writer5.Flush();
                                                        }
                                                    }
                                                }
                                            }
                                        }

                                        //-------------------------ENROLLMENT!
                                        var studentId = "";
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
                                                                studentId = credential.value;
                                                                break;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        //To check final grade
                                        bool found = false;
                                        List<StudentUnverifiedGrades> stcs = await gradeService.GetUGradesByStduent(id);
                                        var gradeValue = "";
                                        foreach (var stc in stcs)
                                        {
                                            if (secRegId == stc.sectionRegistration.id)
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
                                        string line2 = $"{id},{studentId}";
                                        string recordLine3 = $"{couId},{yrtr},{studentId},{droppedDate}";

                                        if (droppedDate.Length > 0)
                                        {
                                            if (enrollmentValues.Add(recordLine3) && season != "AY" &&
                                        (creditType == CRE || creditType == DEV) && !isCancelled)
                                            {
                                                await writer.WriteLineAsync(recordLine3);
                                                Console.WriteLine("Wrote Line to Enrollment " + recordLine3);
                                                _logger.LogString($"Wrote line: {recordLine3}", LogEntryType.Info);
                                                if (uniqueLines.Add(line2)) // Add returns false if already exists
                                                {
                                                    //techids.Add(new TechIds { guid = id, colleagueId = techId });
                                                    writer5.WriteLine(line2);
                                                    writer5.Flush();
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (enrollmentValues.Add(recordLine3) && season != "AY" &&
                                               (creditType == CRE || creditType == DEV) && !isCancelled && gradeValue != gradeN
                                               && gradeValue != gradeNC && gradeValue != gradeNP)
                                            {
                                                await writer.WriteLineAsync(recordLine3);
                                                Console.WriteLine("Wrote Line to Enrollment " + recordLine3);
                                                _logger.LogString($"wrote line: {recordLine3}", LogEntryType.Info);
                                                if (uniqueLines.Add(line2)) // Add returns false if already exists
                                                {
                                                    writer5.WriteLine(line2);
                                                    writer5.Flush();
                                                }
                                            }
                                        }
                                    }
                                }
                                Console.WriteLine("Enrollment.csv, Offering.csv, OfferingSecattr.csv, and FacultyAssignment.csv finished writing");
                            }
                        }
                    }
                }
            }
        }

            static string SanitizeFilename(string filename)
            {
                // Replace / with an underscore or any other character you prefer

                return filename.Replace("&", " ")
                .Replace(",", " ")
                .Replace("/", " ")
                .Replace("'", "")
                .Replace(".", "");
            }
            static string SanitizeFilename2(string filename)
            {
                // Replace / with an underscore or any other character you prefer

                return filename.Replace(";", " ")
                .Replace(",", "")
                .Replace("/", " ")
                .Replace("\\", " ")
                .Replace("!", " ")
                .Replace("@", " ")
                .Replace("%", " ")
                .Replace("*", " ")
                .Replace("(", " ")
                .Replace(")", " ")
                .Replace("[", " ")
                .Replace("]", " ")
                .Replace("_", " ")
                .Replace("+", " ")
                .Replace("{", " ")
                .Replace("}", " ")
                .Replace(":", " ")
                .Replace('"', ' ')
                .Replace("<", " ")
                .Replace(">", " ")
                .Replace("?", " ")
                .Replace("`", " ")
                .Replace("-", " ")
                .Replace("=", " ")
                .Replace("'", "")
                .Replace("\r\n", "")  // Replace Windows line breaks
                .Replace("\n", "")    // Replace Unix line breaks
                .Replace("\r", "");   // Replace Mac line breaks
                                      //.Replace(".", "");
            }
        }
    }


