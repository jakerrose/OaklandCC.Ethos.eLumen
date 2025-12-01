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
using Microsoft.IdentityModel.Tokens;
using static System.Collections.Specialized.BitVector32;
using Section = OaklandCC.Ethos.Common.Models.Section;
using Newtonsoft.Json;

namespace OaklandCC.Ethos.eLumen.Services
{
    public class ElumenCourseService
    {
        private readonly EthosClient _ethosClient;
        private readonly IConfiguration _configuration;
        private readonly Logger _logger;

        /// <summary>  
        /// Initializes a new instance of the <see cref="ElumenCourseService"/> class.  
        /// </summary>  
        /// <param name="ethosClient">The Ethos client for API interactions.</param>  
        /// <param name="configuration">The application configuration settings.</param>  
        /// <param name="logger">The logger for logging messages.</param>  
        public ElumenCourseService(EthosClient ethosClient, IConfiguration configuration, Logger logger)
        {
            _ethosClient = ethosClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task CreateCourseFile()
        {
            // Retrieve file path from appsettings.json  
            string? filePath = _configuration["FileSettings:CourseOutputFilePath"];
            if (string.IsNullOrEmpty(filePath))
            {
                _logger.LogString("Error: File path is not configured in appsettings.json.", LogEntryType.Error);
                return;
            }
            _logger.LogString($"CourseOutputFilePath: {filePath}:", LogEntryType.Info);

            //services
            var deptService = new DeptService(_ethosClient);
            var courseService = new CourseSectionService(_ethosClient);
            var eduService = new EducationalInstitutionUnitService(_ethosClient);
            var subjectService = new SubjectService(_ethosClient,_logger); 

            List<Depts> depts = await deptService.GetAllDeptsAsync(limit: 500);
            // Create a HashSet to store active department IDs (or names, codes, etc.)
            HashSet<string> activeDeptIds = new HashSet<string>();

            List<EducationalInstitutionUnit> eduList = new List<EducationalInstitutionUnit>();
            var json = File.ReadAllText("./units.json");
            eduList = JsonConvert.DeserializeObject<List<EducationalInstitutionUnit>>(json);

            List<Subject> subList = new List<Subject>();
            var json2 = File.ReadAllText("./subjects.json");
            subList = JsonConvert.DeserializeObject<List<Subject>>(json2);

            foreach (var dept in depts)
            {
                if (dept.status == "active")
                {
                    activeDeptIds.Add(dept.code);
                }
            }

            //To ensure no duplicate rows
            HashSet<string> courseValues = new HashSet<string>();

            //date client-provided file was pulled
            //string startOn = "2025-05-09";
            //string activeDate = UserInput.StoredValue3;
            //List<Course> courseList = await courseService.GetAllCoursesPagedAsync(activeDate);
            List<Course> courseList = new List<Course>();
            var courseJson = File.ReadAllText("./courses.json");
            courseList = JsonConvert.DeserializeObject<List<Course>>(courseJson);

            using (StreamWriter writer = new StreamWriter(filePath, append: false))
            {
                // Write header line  
                await writer.WriteLineAsync("Course Subject,Course Number,Course Title,Department,Curriculum Id");

                var CRE = "68ac1a07-440a-4830-99e6-a19b4263f3a8";
                var DEV = "f1d869b2-8920-471b-8c6c-b4b1fab05283";
                Console.WriteLine("Writing ElumenCourse.csv");

                for(int i=0; i<courseList.Count; i++)
                { var course = courseList[i];
                    Console.WriteLine("Writing line " + i +" of "+ courseList.Count);

                    var ciricId = "";
                    if (course.coursesId != null)
                    {
                        ciricId = course.coursesId;
                    }
                    var courseTitle = SanitizeFilename(course.titles[1].value);
                    var courseID = course.id;
                    var courseNo = course.number;
                    var creditType = "";
                    if (creditType != null)
                    {
                        creditType = course.credits[0].creditCategory.detail.id;
                    }
                    bool creditsTrue = false;
                    if (course.credits != null && course.credits[0].minimum>0)
                    {
                        creditsTrue = true;
                    }
                    var id = course.owningInstitutionUnits[0].institutionUnit.id;

                    //to get dept 

                    var dept = "";
                    foreach (var unit in eduList)
                    {
                        if (id == unit.id)
                        {
                            dept = unit.code;
                        }
                    }
                    //to get subject
                    var subjectId = course.subject.id;

                    var subject = "";
                    foreach (var sub in subList)
                    {
                        if (subjectId == sub.id)
                        {
                            subject = sub.abbreviation;
                            break;
                        }
                    }
                    string recordLine = $"{subject},{courseNo},{courseTitle},{dept},{ciricId}";
                    Console.WriteLine(recordLine);
                    _logger.LogString($"line to test: {recordLine}:", LogEntryType.Info);
                    Console.WriteLine("credit type:" + creditType);
                    Console.WriteLine("credits true?: " + creditsTrue);
                    Console.WriteLine("dept: " + dept);
                    if ((creditType == CRE||creditType == DEV) && creditsTrue && activeDeptIds.Contains(dept,StringComparer.OrdinalIgnoreCase))
                        {
                        await writer.WriteLineAsync(recordLine);                       
                            if (courseValues.Add(recordLine))
                            {
                                
                                _logger.LogString($"recordLine: {recordLine}:", LogEntryType.Info);
                                Console.Write(recordLine);
                            }
                        }
                }
                Console.WriteLine("ElumenCourse.csv finished writing");
                
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
        }
    }
}