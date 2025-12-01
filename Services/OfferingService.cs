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
    public class OfferingService
    {
        private readonly EthosClient _ethosClient;
        private readonly IConfiguration _configuration;
        private readonly Logger _logger;
        /// <summary>  
        /// Initializes a new instance of the <see cref="OfferingService"/> class.  
        /// </summary>  
        /// <param name="ethosClient">The Ethos client for API interactions.</param>  
        /// <param name="configuration">The application configuration settings.</param>  
        /// <param name="logger">The logger for logging messages.</param>  
        public OfferingService(EthosClient ethosClient, IConfiguration configuration, Logger logger)
        {
            _ethosClient = ethosClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task CreateOfferingFile()
        {

            //to write OFFERING.csv
            //var sectionService = new CourseSectionService(_ethosClient);
            //var secRegService = new SectionRegistrationService(_ethosClient);
            var acadService = new AcademicPeriodService2(_ethosClient, _logger);
            //var eduService = new EducationalInstitutionUnitService(_ethosClient);
            var courseService = new CourseSectionService(_ethosClient);

            //to ensure no duplicates
            HashSet<string> offeringValues = new HashSet<string>();

            //using json file
            List<SectionById> sectionList = new List<SectionById>();
            var json = File.ReadAllText("./sections.json");
            sectionList = JsonConvert.DeserializeObject<List<SectionById>>(json);
            Console.WriteLine("Sections loaded: " + sectionList.Count);

            List<EducationalInstitutionUnit> eduList = new List<EducationalInstitutionUnit>();
            var json2 = File.ReadAllText("./units.json");
            eduList = JsonConvert.DeserializeObject<List<EducationalInstitutionUnit>>(json2);
            Console.WriteLine("Units loaded: " + eduList.Count);

            List<SectionRegistration> secRegList = new List<SectionRegistration>();
            var json8 = File.ReadAllText("./sectionRegistrations.json");
            secRegList = JsonConvert.DeserializeObject<List<SectionRegistration>>(json8);

            // Retrieve file path from appsettings.json  
            string? filePath = _configuration["FileSettings:OfferingOutputFilePath"];
            if (string.IsNullOrEmpty(filePath))
            {
                _logger.LogString("Error: File path is not configured in appsettings.json.", LogEntryType.Error);
                return;
            }
            _logger.LogString($"OfferingOutputFilePath: {filePath}:", LogEntryType.Info);

            // Retrieve section
            var periodId = "b901962d-2f32-4050-b09c-de714a81ea7f";
            var academicPeriodId = periodId;

            //academic period drop date
            AcademicPeriod2 academicPeriod = await acadService.GetAcademicPeriodByIdAsync(academicPeriodId);
            var termDropStart = academicPeriod.termDropStartDate.Replace("-", "");
            int dropStart = Int32.Parse(termDropStart);

            //List<Section> sectionList = await sectionService.GetAllSectionsPagedAsync(periodId, pageSize: 100);
            using (StreamWriter writer = new StreamWriter(filePath, append: false))
            {
                // Write header line  
                await writer.WriteLineAsync("CURRIC_ID,SUBJ,COU_NBR,TITLE,DS,ORG_OWNER_ID,COU_ID,YRTR,SECT_NBR, COU_TYPE,CAMPUSID,DELETE_SECTION");


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
                        if (secRegStausDetail == W || secRegStausDetail == D )
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
                            //SectionById sectionList = await sectionService.GetSectionsByIdAsync(sectionId);
                            //check instructional methods for cancelled

                            var cancelled = "e8ab394d-b5bf-42bd-834a-3e973664eb48";
                            bool isCancelled = false;
                            var courseId = "";
                            var creditType = "";
                            var couId = "";
                            var sectNumber = "";
                            var subject = "";
                            var courseNo = "";
                            var yrtr = "";
                            var id = "";
                          
                            foreach (var section in sectionList)
                            {
                                if (secReg.section.id == section.id)
                                {
                                    courseId = section.course.id;
                                                                      
                                    if (section.credits != null)
                                    {
                                        creditType = section.credits[0].creditCategory.detail.id;
                                    }

                                    couId = section.courseSectionsId;
                                    sectNumber = section.number;
                                    subject = section.code.Split("-")[0];
                                    courseNo = section.code.Split("-")[1];
                                    yrtr = section.termCode.Replace("/", "");

                                    id = section.owningInstitutionUnits[0].institutionUnit.id;
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
                                if (id == edus.id)
                                {
                                    orgOwnerId = edus.code;
                                    break;
                                }
                            }

                            //to get description, title, and ciric id                                
                            //CourseById courseList = await courseService.GetCoursesByIdAsync(id: $"{courseId}");
                            var ciricID = "";
                            var desc = "";
                            var title = "";

                            List<CourseById> courseList = new List<CourseById>();
                            var json3 = File.ReadAllText("./courses.json");
                            courseList = JsonConvert.DeserializeObject<List<CourseById>>(json3);
                            Console.WriteLine("Courses loaded: " + courseList.Count);


                            //for using json file
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

                            //credit categories
                            var CRE = "68ac1a07-440a-4830-99e6-a19b4263f3a8";
                            var DEV = "f1d869b2-8920-471b-8c6c-b4b1fab05283";
                            
                                var couType = "";
                                var campId = "";
                                var delete = "";
                                if (!isCancelled)
                                {

                                    //// Write the record line  
                                    //CURRIC_ID,SUBJ,COU_NBR,TITLE,DS,ORG_OWNER_ID,COU_ID,YRTR,SECT_NBR, COU_TYPE,CAMPUSID,DELETE_SECTION
                                    string recordLine = $"{ciricID.Trim()},{subject.Trim()},{courseNo.Trim()},{title.Trim()},{desc.Trim()},{orgOwnerId.Trim()},{couId.Trim()},{yrtr.Trim()}," +
                                        $"{sectNumber},{couType},{campId},{delete}";
                                if (offeringValues.Add(recordLine) && (creditType == CRE || creditType == DEV))
                                {
                                    await writer.WriteLineAsync(recordLine);
                                    _logger.LogString($"recordLine: {recordLine}: index: {i}", LogEntryType.Info);
                                    Console.Write(recordLine);
                                }

                            }
                            else
                            {
                                continue;
                            }
                        }
                        else
                        {
                            continue;
                        }

                    }
                    else
                    {
                        continue;
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
}
    
