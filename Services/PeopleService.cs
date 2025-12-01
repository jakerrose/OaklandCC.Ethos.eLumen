using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using OaklandCC.Ethos.Common;
using OaklandCC.Ethos.Common.Models;
using OaklandCC.Ethos.Common.Services;

namespace OaklandCC.Ethos.eLumen.Services
{
    /// <summary>  
    /// This class provides functionality to create PERSON AND DEMOGRAPHICDATA files  
    /// It retrieves the file path from configuration settings and processes the data accordingly.  
    /// </summary>  
    public class PeopleService
    {
        private readonly EthosClient _ethosClient;
        private readonly IConfiguration _configuration;
        private readonly Logger _logger;

        /// <summary>  
        /// Initializes a new instance of the <see cref="PeopleService"/> class.  
        /// </summary>  
        /// <param name="ethosClient">The Ethos client for API interactions.</param>  
        /// <param name="configuration">The application configuration settings.</param>  
        /// <param name="logger">The logger for logging messages.</param>  
        public PeopleService(EthosClient ethosClient, IConfiguration configuration, Logger logger)
        {
            _ethosClient = ethosClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task CreatePersonFile()
        {
            //To ensure no duplicate rows
            HashSet<string> personValues = new HashSet<string>();
            HashSet<string> demoValues = new HashSet<string>();

            //var term = "b901962d-2f32-4050-b09c-de714a81ea7f";
            
            //services
            var resService = new StudentService(_ethosClient);
            var personService = new PersonService(_ethosClient);

            var academicPeriodId = UserInput.StoredAcadPeriod;
            var TermCode = UserInput.StoredAcadCode;
            var newTermCode = TermCode.Replace("/", "");

            string baseFileName = _configuration["FileSettings:PersonOutputFilePath"] ?? "PERSON.csv";
            string filePath = Path.GetFileNameWithoutExtension(baseFileName) + $"_{newTermCode}" + Path.GetExtension(baseFileName);
            string baseFileName2 = _configuration["FileSettings:DemographicDataOutputFilePath"] ?? "DEMOGRAPHICDATA.csv";
            string filePath2 = Path.GetFileNameWithoutExtension(baseFileName2) + $"_{newTermCode}" + Path.GetExtension(baseFileName2);

            var techids = new List<TechIds>();
            var lines = File.ReadAllLines("techids.csv");
            Console.WriteLine("Gathering person Ids for Person.csv and DemographicData.csv");
            for (int i = 1; i < lines.Length; i++) // skip header
            {
                var parts = lines[i].Split(',');

                if (parts.Length == 2)
                {
                    var techId = new TechIds
                    {
                        guid = parts[0].Trim(),
                        colleagueId = parts[1].Trim()
                    };

                    techids.Add(techId);
                }
            }

            //--------------for PERSON.csv

            using (StreamWriter writer = new StreamWriter(filePath, append: false))
            {
                // Write header line PERSON.csv 
                await writer.WriteLineAsync("TECH_ID,FIRST_NAME,LAST_NAME,MIDDLE_NAME,INTERNET_ADDR,LOGIN_ID,IS_STUDNT,IS_FACULTY,Age Category,Gender,Race Ethnicity,Residency Status");
//--------------for DEMOGRAPHICDATA.csv
                using (StreamWriter writer2 = new StreamWriter(filePath2, append: false))
                {
                    // Write header line DEMOGRAPHICDATA.csv
                    await writer2.WriteLineAsync("TECH_ID,DEMOGRAPHIC_CATEGORY,DEMOGRAPHIC_DATA");

                    try
                    {
                        Console.WriteLine("Writing Person.csv and DemographicData.csv");
                        //loops through techIds
                        for (int i = 0; i < techids.Count; i++)
                        {
                            Console.WriteLine("Processing line " + i + " of " + techids.Count);
                            var techId = techids[i];
                            var regId = techId.guid;
                            var colleagueId = techId.colleagueId;

                            Console.WriteLine("index: " + i);

                                    // Retrieve sites  
                                    Person person = await personService.GetPersonByIdAsync(regId);

                                    //for is_student, is_faculty    
                                    var studentRole = "N";
                                    var facultyRole = "N";

                                    if (person.roles != null)
                                    {
                                        foreach (var r in person.roles)
                                        {
                                            if (r.role == "student")
                                            {
                                                studentRole = "Y";
                                            }
                                            else if (r.role == "instructor")
                                            {
                                                facultyRole = "Y";
                                            }

                                            // Optional early exit if both roles are found
                                            if (studentRole == "Y" && facultyRole == "Y")
                                                break;
                                        }
                                    }
                                    var ageCategory = "";
                                    if (person.dateOfBirth != null)
                                    {
                                        //Logic for Age Category
                                        var dob = person.dateOfBirth;
                                        string[] dobParts = dob.Split("-");
                                        var dobYear = dobParts[0];
                                        int dobYearNum = Int32.Parse(dobYear);
                                        var dobMonth = dobParts[1];
                                        int dobMonthNum = Int32.Parse(dobMonth);
                                        var dobDay = dobParts[2];
                                        int dobDayNum = Int32.Parse(dobDay);
                                        int dobMonthDay = (dobMonthNum * 100) + dobDayNum;
                                //use current date
                                var currentDate = DateTime.Today.ToString().Split(" ")[0];
                                string[] currentDateParts = currentDate.Split("/");
                                var curYear = currentDateParts[2];
                                int curYearNum = Int32.Parse(curYear);
                                var curMonth = currentDateParts[0];
                                int curMonthNum = Int32.Parse(curMonth);
                                var curDay = currentDateParts[1];
                                int curDayNum = Int32.Parse(curDay);
                                int curMonthDay = (curMonthNum * 100) + curDayNum;
                                //use saved date
                                //var currentDate = UserInput.StoredValue2;                                       
                                //string[] currentDateParts = currentDate.Split("-");
                                //var curYear = currentDateParts[0];
                                //int curYearNum = Int32.Parse(curYear);
                                //var curMonth = currentDateParts[1];
                                //int curMonthNum = Int32.Parse(curMonth);
                                //var curDay = currentDateParts[2];
                                //int curDayNum = Int32.Parse(curDay);
                                //int curMonthDay = (curMonthNum * 100) + curDayNum;



                                int age = curYearNum - dobYearNum;
                                        if (curMonthDay < dobMonthDay)
                                            age = age - 1;

                                        if (person.dateDeceased != null)
                                        {
                                            var deceasedDate = person.dateDeceased;
                                            string[] decParts = deceasedDate.Split("-");
                                            var decYear = decParts[0];
                                            int decYearNum = Int32.Parse(decYear);
                                            var decMonth = decParts[1];
                                            int decMonthNum = Int32.Parse(decMonth);
                                            var decDay = decParts[2];
                                            int decDayNum = Int32.Parse(decDay);
                                            int decMonthDay = (decMonthNum * 100) + decDayNum;

                                            age = decYearNum - dobYearNum;
                                            if (decMonthDay > dobMonthDay) age = age - 1;
                                        }

                                        if (age < 18)
                                            ageCategory = "Under 18 yrs";
                                        else if (age >= 18 && age < 25)
                                            ageCategory = "18 - 24 yrs";
                                        else if (age >= 25 && age < 40)
                                            ageCategory = "25 - 39 yrs";
                                        else if (age >= 40 && age < 50)
                                            ageCategory = "40 - 49 yrs";
                                        else if (age >= 50)
                                            ageCategory = "50 or older";
                                    }
                                    else { ageCategory = "Unknown";}

                                //for gender
                                var gender = "";
                                    if (person.gender != null)
                                    {
                                        if (person.gender == "male")
                                        { gender = "Male"; }
                                        else if (person.gender == "female")
                                        { gender = "Female"; }
                                        
                                    }
                                    else
                                    {
                                        gender = "Not Reported";
                                    }

                                //for racial category
                                var race = "";
                                var raceId = "";
                            bool isHispanic = false;
                            if (person.ethnicity != null)
                            {
                                foreach (var category in person.ethnicity.reporting)
                                {
                                    if (category.country.ethnicCategory == "hispanic")
                                    {
                                        race = "Hispanic";
                                        isHispanic = true;
                                    }
                                }
                            }
                            if (person.races != null && !isHispanic)
                            {
                                if (person.races.Length > 1)
                                {
                                    race = "Two or more races";
                                }
                                else if (person.races[0].race.id != null)
                                {
                                    raceId = person.races[0].race.id;
                                    if (raceId == "9100a2b9-acd2-432b-b181-5af2ef2fe2c3")
                                        race = "American Indian or Alaska Native";
                                    else if (raceId == "1da7a229-2754-4eb6-a02f-bbd1d10c4dd5")
                                        race = "Asian";
                                    else if (raceId == "51509c9c-7a9a-4c51-bd33-6eb3ba89aa34")
                                        race = "Black or African American";
                                    else if (raceId == "6871ae1d-2250-44b5-a43a-707fb3426e4a")
                                        race = "Native Hawaiian or Pacific Islander";
                                    else if (raceId == "7cafb047-10ec-4fbb-8143-2c7721e538b0")
                                        race = "White";
                                    else if (raceId == "892e2e9e-c820-4235-a624-542a0806a1e8")
                                        race = "Native Hawaiian or Pacific Islander";
                                }
                                else if (person.races[0].reporting[0].country.racialCategory == "americanIndianOrAlaskaNative")
                                    race = "American Indian or Alaska Native";
                                else if (person.races[0].reporting[0].country.racialCategory == "asian")
                                    race = "Asian";
                                else if (person.races[0].reporting[0].country.racialCategory == "blackOrAfricanAmerican")
                                    race = "Black or African American";
                                else if (person.races[0].reporting[0].country.racialCategory == "hispanic")
                                    race = "Hispanic";
                                else if (person.races[0].reporting[0].country.racialCategory == "nativeHawaiianOrPacificIslander")
                                    race = "Native Hawaiian or Pacific Islander";
                                else if (person.races[0].reporting[0].country.racialCategory == "international")
                                    race = "International";
                                else if (person.races[0].reporting[0].country.racialCategory == "twoOrMoreRaces")
                                    race = "Two or more races";
                                else if (person.races[0].reporting[0].country.racialCategory == "white")
                                    race = "White";
                                else if (person.races[0].reporting[0].country.racialCategory == "" || person.races[0].reporting[0].country.racialCategory == "unknown")
                                    race = "Unknown";
                                else
                                {
                                    race = "Unknown";
                                }
                            }
                            if(person.races == null && !isHispanic)
                            {
                                if (person.archivedEthnicity != null)
                                {
                                    if (person.archivedEthnicity == "WH")
                                    {
                                        race = "White";
                                    }
                                    else if (person.archivedEthnicity == "AF"|| person.archivedEthnicity == "BL")
                                    {
                                        race = "Black or African American";
                                    }
                                    else if (person.archivedEthnicity == "HI"|| person.archivedEthnicity == "HIS")
                                    {
                                        race = "Hispanic";
                                    }
                                    else if (person.archivedEthnicity == "UN")
                                    {
                                        race = "Unknown";
                                    }
                                    if (person.archivedEthnicity == "AN" || person.archivedEthnicity == "NI")
                                    {
                                        race = "American Indian or Alaska Native";
                                    }
                                    if(person.archivedEthnicity =="AS")
                                    {
                                        race = "Asian";
                                    }
                                    if (person.archivedEthnicity == "HP" || person.archivedEthnicity == "NP")
                                    {
                                        race = "Native Hawaiian or Pacific Islander";
                                    }
                                }
                                else
                                {
                                    race = "Unknown";
                                }
                            }
                            
                            if (person.perRaces != null && !isHispanic)
                            {
                                if (person.perRaces.Count > 1)
                                {
                                    race = "Two or more races";
                                }
                                else
                                { 
                                    foreach (var perRace in person.perRaces)
                                    {
                                        if (perRace == "UN")
                                        {
                                            race = "Unknown";
                                        }
                                        if (perRace == "AN" || perRace == "NI")
                                        {
                                            race = "American Indian or Alaska Native";
                                        }
                                        if (perRace == "BL" || perRace == "AF")
                                        {
                                            race = "Black or African American";
                                        }
                                        if(perRace == "AS")
                                        {
                                            race = "Asian";
                                        }
                                        if (perRace == "HP" || perRace == "NP")
                                        {
                                            race = "Native Hawaiian or Pacific Islander";
                                        }
                                        if(perRace == "WH")
                                        {
                                            race = "White";
                                        }
                                    }
                                }
                            }
        
                            if (person.citizenshipStatus!= null)
                            {
                                var nonCitizen = "5c4162f1-0375-4539-83a7-9f60f1f8235d";
                                //var nonCitizen2 = "97589fc6-5238-42ea-ba7e-2906337ccbde";
                                var nonCitizen3 = "0fb6a3c0-d069-4eb2-af9b-eab3ed9acbd0";
                                var nonCitizen4 = "2ced184b-ae00-4a83-bd56-dc0df51e0fdb";
                                var nonCitizen5 = "edbb6135-5ded-40ee-badc-9c24ce1983ca";
                                var nonCitizen6 = "5c856ada-1e9b-4ae6-8188-3fcd6d772a33";
                                var nonCitizen7 = "a6478b36-52b9-4855-900b-8e5259f1ffd6";
                                var nonCitizen8 = "ab3ec1cc-6da7-4502-b7b9-2a3c204062ba";
                                var nonCitizen9 = "96611ebb-5784-4fc7-86ee-6d00f91e1fef";

                                if (person.citizenshipStatus.detail.id == nonCitizen || 
                                    //person.citizenshipStatus.detail.id == nonCitizen2||
                                    person.citizenshipStatus.detail.id == nonCitizen3||
                                    person.citizenshipStatus.detail.id == nonCitizen4 ||
                                    person.citizenshipStatus.detail.id == nonCitizen5 ||
                                    person.citizenshipStatus.detail.id == nonCitizen6 ||
                                    person.citizenshipStatus.detail.id == nonCitizen7 ||
                                    person.citizenshipStatus.detail.id == nonCitizen8 ||
                                    person.citizenshipStatus.detail.id == nonCitizen9
                                    )
                                {
                                    race = "International";
                                }
                            }
                            if (person.immigrationStatus != null)
                            {
                                if (person.immigrationStatus == "AN" || person.immigrationStatus == "NI")
                                {
                                    race = "American Indian or Alaska Native";
                                }
                                if (person.immigrationStatus == "AS")
                                {
                                    race = "Asian";
                                }
                                if (person.immigrationStatus == "BL" || person.immigrationStatus == "AF")
                                {
                                    race = "Black or African American";
                                }
                                if (person.immigrationStatus == "HP" || person.immigrationStatus == "NP")
                                {
                                    race = "Native Hawaiian or Pacific Islander";
                                }
                                if (person.immigrationStatus == "WH")
                                {
                                    race = "White";
                                }
                                if (person.immigrationStatus == "HI" || person.immigrationStatus == "HIS")
                                {
                                    race = "Hispanic";
                                }
                                if (person.immigrationStatus == "UN")
                                {
                                    race = "Unknown";
                                }
                                if (new[] { "PR", "TR", "RE", "RA", "NR", "AU", "F1", "OI", "DACA", "IDNC",
                                "TI20", "VISA", "Y"}.Contains(person.immigrationStatus))
                                {
                                    race = "International";
                                }
                            }


                            //for residency status
                            var resStatus = "";
                                    //var resService = new StudentService(_ethosClient);
                                    StudentResidency resPerson = await resService.GetStudentByPersonId(personId: $"{person.id}");
                                    if (resPerson != null && resPerson.residencies != null && resPerson.residencies.Any())
                                    {
                                        foreach (var residency in resPerson.residencies)
                                        {
                                            var residencyId = residency.residency?.id;

                                            if (!string.IsNullOrEmpty(residencyId))
                                            {
                                                Console.WriteLine($"Residency ID: {residencyId}");

                                                resStatus = residencyId;
                                                break;
                                            }
                                        }
                                    }
                                    if (resStatus != null)
                                    {
                                        //12 residency types
                                        if (resStatus == "850f9d6d-5380-4085-af00-2d2cb43930ed")
                                            resStatus = "In-District";
                                        else if (resStatus == "0d405224-58c6-4bc3-b05b-d636feb9d919")
                                            resStatus = "Virtual College In District";
                                        else if (resStatus == "ac9ed772-9dc6-455c-a4dc-280fcd7e36ee")
                                            resStatus = /*"International In District"*/ "In-District";
                                        else if (resStatus == "283a1d7f-2acb-4141-8213-bc8d8e1c4fbe")
                                            resStatus = /*"International Out of District"*/ "Out-of-District";
                                        else if (resStatus == "f9fcf8ef-69db-4046-9b8f-2d547bb6e76d")
                                            resStatus = /*"International Out of State"*/ "Out-of-District";
                                        else if (resStatus == "c7272d69-489c-4745-b234-2274a7d5e29b")
                                            resStatus = "Out-of-District";
                                        else if (resStatus == "a4a9b3e9-fda4-41b9-9acd-c2b97347e82f")
                                            resStatus = "Virtual College Out of District";
                                        else if (resStatus == "a196c4a7-3078-40e8-8d21-4f0dc5ea5f5e")
                                            resStatus = /*"Out of State"*/ "Out-of-District";
                                        else if (resStatus == "0751f08e-dbf7-429e-87e5-e39c61db6519")
                                            resStatus = "Virtual College out of State";
                                        else if (resStatus == "5c9f7c10-8e57-48d6-8510-7f1ac6c7de87")
                                            resStatus = /*"Residency Verification Needed"*/ "Out-of-District";
                                        else if (resStatus == "2ea725a2-382f-4b87-a596-a3a395283b35")
                                            resStatus = "Do Not Use";
                                        else if (resStatus == "c3012370-960e-4e8f-a208-47d8d601610e")
                                            resStatus = "Do Not Use";
                                    }
                                    if(resPerson.residencies == null)
                                    {
                                        resStatus = "Not Reported";
                                    }
                                    


                                var loginId = "";
                                    if (person.credentials != null)
                                    {
                                        foreach (var credential in person.credentials)
                                        {
                                            switch (credential.type)
                                            {
                                                case "colleagueUserName":
                                                    loginId = credential.value;
                                                    break;
                                            }
                                        }
                                    }
                                    var firstName = "";
                                    if (person.names[0].firstName != null)
                                        firstName = SanitizeFirstname(person.names[0].firstName.Trim());
                                    var lastName = "";
                                    if (person.names[0].lastName != null)
                                        lastName = SanitizeFilename(person.names[0].lastName.Trim());
                                    var middleName = "";
                                    if (person.names[0].middleName != null)
                                        middleName = SanitizeMiddlename(person.names[0].middleName.Trim());
                                    var intAddr = "";
                                    if (person.emails != null)
                                        intAddr = person.emails[0].address;

                                    // Write the record line
                                    // TECH_ID,FIRST_NAME,LAST_NAME,MIDDLE_NAME,INTERNET_ADDR, LOGIN_ID,IS_STUDNT,IS_FACULTY, Age Category,Gender,Race Ethnicity,Residency Status
                                    string personLine = $"{colleagueId},{firstName},{lastName},{middleName},{intAddr}," +
                                                $"{loginId},{studentRole},{facultyRole},{ageCategory},{gender},{race},{resStatus}";
                                    Console.WriteLine(personLine);
                                    string demoLine1 = $"{colleagueId},Gender,{gender}";
                                    string demoLine2 = $"{colleagueId},Race Ethnicity,{race}";
                                    string demoLine3 = $"{colleagueId},Age Category,{ageCategory}";
                                    string demoLine4 = $"{colleagueId},Residency Status,{resStatus}";

                                    if (personValues.Add(personLine))
                                    {
                                        await writer.WriteLineAsync(personLine);
                                        _logger.LogString($"recordLine: {personLine}: index: {i}", LogEntryType.Info);
                                    }
                                    if (demoValues.Add(demoLine1))
                                    {
                                        await writer2.WriteLineAsync(demoLine1);
                                        _logger.LogString($"recordLine: {demoLine1}: index: {i}", LogEntryType.Info);
                                    }
                                    if (demoValues.Add(demoLine2))
                                    {
                                        await writer2.WriteLineAsync(demoLine2);
                                        _logger.LogString($"recordLine: {demoLine2}: index: {i}", LogEntryType.Info);
                                    }
                                    if (demoValues.Add(demoLine3))
                                    {
                                        await writer2.WriteLineAsync(demoLine3);
                                        _logger.LogString($"recordLine: {demoLine3}: index: {i}", LogEntryType.Info);
                                    }
                                    if (demoValues.Add(demoLine4))
                                    {
                                        await writer2.WriteLineAsync(demoLine4);
                                        _logger.LogString($"recordLine: {demoLine4}: index: {i}", LogEntryType.Info);
                                    }
                                }
                            }                   
                    catch (NullReferenceException ex)
                    {
                        Console.WriteLine("NullReferenceException caught: " + ex.Message);
                        // Optionally log which section failed for easier tracing

                    }
                }
            }
            Console.WriteLine("Person.csv and DemographicData.csv finished writing");
        }
        static string SanitizeFilename(string filename)
        {
            // Replace / with an underscore or any other character you prefer

            return filename.Replace("&", " ")
            .Replace(",", " ")
            .Replace("/", " ")
            .Replace(".", "")
            .Replace("é", "e")
            .Replace("ñ", "n")
            .Replace("á", "a")
            .Replace("í", "i");
        }
        static string SanitizeFirstname(string filename)
        {
            // Replace / with an underscore or any other character you prefer

            return filename.Replace("&", " ")
            .Replace("’", "")
            .Replace(",", " ")
            .Replace("/", " ")
            .Replace(".", "")
            .Replace("é", "e")
            .Replace("ñ", "n")
            .Replace("á", "a")
            .Replace("í", "i");
        }
        static string SanitizeMiddlename(string filename)
        {
            // Replace / with an underscore or any other character you prefer

            return filename.Replace("&", " ")
            .Replace("’", "")
            .Replace(",", " ")
            .Replace("/", " ")
            .Replace("é", "e")
            .Replace("ñ", "n")
            .Replace("á", "a")
            .Replace("í", "i");
        }
    }
}

