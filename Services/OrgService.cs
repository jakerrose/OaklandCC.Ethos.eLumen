using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using OaklandCC.Ethos.Common;
using OaklandCC.Ethos.Common.Models;
using OaklandCC.Ethos.Common.Services;

namespace OaklandCC.Ethos.eLumen.Services
{
    /// <summary>  
    /// This class provides functionality to create a campus file.  
    /// It retrieves the file path from configuration settings and processes the data accordingly.  
    /// </summary>  
    public class OrgService
    {
        private readonly EthosClient _ethosClient;
        private readonly IConfiguration _configuration;
        private readonly Logger _logger;

        /// <summary>  
        /// Initializes a new instance of the <see cref="OrgService"/> class.  
        /// </summary>  
        /// <param name="ethosClient">The Ethos client for API interactions.</param>  
        /// <param name="configuration">The application configuration settings.</param>  
        /// <param name="logger">The logger for logging messages.</param>  
        public OrgService(EthosClient ethosClient, IConfiguration configuration, Logger logger)
        {
            _ethosClient = ethosClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task CreateOrgFile()
        {
            //create set of active dept codes
            //Query Stmt: ‘MIOSEL DEPTS WITH DEPTS.ACTIVE.FLAG EQ "A" AND DEPTS.TYPE NE "H" AND DEPTS.DIVISION NE ""’
            //Retrieves all depts to filter for status: "active". Depts.Division should correlate to educational-institution-units.code
            //DEPTS.TYPE NE "H" still unsure about, types I can see are "department" and "division"

            var deptService = new DeptService(_ethosClient);
            List<Depts> depts = await deptService.GetAllDeptsAsync(limit: 500);
            // Create a HashSet to store active department IDs (or names, codes, etc.)
            HashSet<string> activeDeptIds = new HashSet<string>();

            foreach (var dept in depts)
            {
                if (dept.status == "active")
                {
                    activeDeptIds.Add(dept.code);
                }
            }

            //code to create virtual Dept Locate Table
            var miscService = new MiscService(_ethosClient);
            List<MiscText> elementList = await miscService.GetAllTextAsync(limit: 500);

            HashSet<MiscText> miscIds = new HashSet<MiscText>();
            Console.WriteLine("Creating Org.csv");
            // Process each location  
            foreach (var element in elementList)
                if (element.id == "misc.text-2belumen_dept")
                {
                    var input = element.text;
                    {
                        // Split entries by space only if they separate records
                        var entries = Regex.Matches(input, @"([A-Z]{2,3})\|([A-Z]{4})\|(.+?)(?=(?:\s+[A-Z]{2,3}\|)|$)");
                        foreach (Match match in entries)
                        {
                            var category = match.Groups[1].Value.Trim();
                            var elementCode = match.Groups[2].Value.Trim();
                            var value = match.Groups[3].Value.Trim();

                            miscIds.Add(new MiscText
                            {
                                Category = category,
                                ElementCode = elementCode,
                                Value = value
                            });
                            foreach (var entry in miscIds)
                            {
                                Console.WriteLine($"Category: {entry.Category}, ElementCode: {entry.ElementCode}, Value: {entry.Value}");
                            }
                        }
                    }
                }


            var orgService = new EducationalInstitutionUnitService(_ethosClient);

            // Retrieve file path from appsettings.json  
            string? filePath = _configuration["FileSettings:OrgOutputFilePath"];
            if (string.IsNullOrEmpty(filePath))
            {
                _logger.LogString("Error: File path is not configured in appsettings.json.", LogEntryType.Error);
                return;
            }
            _logger.LogString($"CampusOutputFilePath: {filePath}:", LogEntryType.Info);

            // Retrieve sites  
            List<EducationalInstitutionUnit> orgList = await orgService.GetAllUnitsAsync(limit: 500);

            using (StreamWriter writer = new StreamWriter(filePath, append: false))
            {
                // Write header line  
                await writer.WriteLineAsync("DEPT_ID,SHORT_DESC,LONG_DESC,CAMPUS_ID,ORGTYPE,PARENTORGID");
                // Process each location  
                foreach (var org in orgList)
                {
                    string campusId = "";
                    if(org.deptsLocations != null)
                    {
                        campusId = org.deptsLocations.Trim();
                    }
                    //DEPT
                    if (org.type == "department")
                    {
                        var parentOrg = "";
                        if (activeDeptIds.Contains(org.code))
                        {
                            var desc = SanitizeFilename(org.title);
                            
                            bool found = false;

                            foreach (var miscId in miscIds)
                            {
                                if (org.code == miscId.Category)
                                {
                                    parentOrg = miscId.ElementCode;
                                    found = true;
                                    break;
                                }
                            }
                            if (!found)
                            {
                                if (org.deptsDivision != null)
                                {
                                    parentOrg = org.deptsDivision;
                                }
                                else
                                {
                                    parentOrg = "";
                                }
                            }
                            // Write the record line  
                            string recordLine = $"{org.code.Trim()},{desc.Trim()},{desc.Trim()},{campusId},{"Program"},{parentOrg.Trim()}";
                            await writer.WriteLineAsync(recordLine);
                            Console.WriteLine(recordLine);
                            _logger.LogString($"recordLine: {recordLine}:", LogEntryType.Info);
                        }
                        else { continue; }
                    }
                    //DIVISIONS
                    if (org.type == "division")
                    {
                        var desc = SanitizeFilename(org.title);
                        var parentOrg = "";
                            if (org.deptsDivision != null)
                        {
                            parentOrg = org.deptsDivision;
                        }


                        // Write the record line  
                        string recordLine = $"{org.code.Trim()},{desc.Trim()},{desc.Trim()},{campusId},{"Department"},{parentOrg.Trim()}";
                        await writer.WriteLineAsync(recordLine);
                        _logger.LogString($"recordLine: {recordLine}:", LogEntryType.Info);
                    }
                }
                Console.WriteLine("Org.csv finished writing");
            }
        }
    
        static string SanitizeFilename(string filename)
        {
            // Replace / with an underscore or any other character you prefer

            return filename.Replace(" & ","  ")
            .Replace(",","")
            .Replace("'","")
            .Replace(".","");
        }
    }
}
