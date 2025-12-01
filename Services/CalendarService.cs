using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
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
    public class CalendarService
    {
        private readonly EthosClient _ethosClient;
        private readonly IConfiguration _configuration;
        private readonly Logger _logger;
        //private readonly ProcessingTermsCache _processingTermsCache;

        /// <summary>  
        /// Initializes a new instance of the <see cref="CalendarService"/> class.  
        /// </summary>  
        /// <param name="ethosClient">The Ethos client for API interactions.</param>  
        /// <param name="configuration">The application configuration settings.</param>  
        /// <param name="logger">The logger for logging messages.</param>  
        public CalendarService( EthosClient ethosClient, IConfiguration configuration, Logger logger)
        {
            _ethosClient = ethosClient;
            _configuration = configuration;
            _logger = logger;

            //_processingTermsCache = processingTermsCache;
            
        }

        /// <summary>  
        /// Creates a demographics file based on active academic periods.  
        /// </summary>  
        /// <returns>A task representing the asynchronous operation.</returns>  
        /// <remarks>  
        /// This method retrieves the file path from the configuration settings and checks for active academic periods.  
        /// If no active periods are found, it logs a warning and returns.   
        /// It processes the data and writes it to the specified file.  
        /// </remarks>  
        public async Task CreateCalendarFile()
        {
            var acadService = new AcademicPeriodService2( _ethosClient, _logger);//, _processingTermsCache);

            // Retrieve file path from appsettings.json  
            string? filePath = _configuration["FileSettings:CalendarOutputFilePath"];
            if (string.IsNullOrEmpty(filePath))
            {
                _logger.LogString("Error: File path is not configured in appsettings.json.", LogEntryType.Error);
                return;
            }
            _logger.LogString($"CampusOutputFilePath: {filePath}:", LogEntryType.Info);

            // Retrieve sites  
            List<AcademicPeriod2> acadList = await acadService.GetAllAcademicPeriodsAsync(limit: 100);

            using (StreamWriter writer = new StreamWriter(filePath, append: false))
            {
                // Write header line  
                await writer.WriteLineAsync("YRTR,SHORT_DESC,LONG_DESC,BEGIN_DATE,END_DATE");
                // Process each location
                Console.WriteLine("Writing Calendar.csv");
                foreach (var acad in acadList)
                {
                    // Write the record line
                    var code = acad.code.Split("/")[1];
                    var yrtr = acad.title.Split(' ')[1]+code.ToUpper();
                    var startOn = acad.startOn.ToString().Split(" ")[0];
                    string[] startOnParts = startOn.Split("/");

                    if (startOnParts[0].Length ==1)
                    {
                        startOnParts[0] = startOnParts[0].Insert(0, "0");
                    }
                    if (startOnParts[1].Length < 2)
                    {
                        startOnParts[1] = startOnParts[1].Insert(0, "0");
                    }
                    var newStartOn = startOnParts[2] + startOnParts[0]+startOnParts[1];
                    //output returns the previous day
                    //int newStartOnNum = Int32.Parse(newStartOn);
                    //newStartOnNum = newStartOnNum + 1;

                    var endDate = acad.endOn;
                    var endOn = acad.endOn.ToString().Split(" ")[0];
                    string[] endOnParts = endOn.Split("/");
                    if (endOnParts[0].Length == 1)
                    {
                        endOnParts[0] = endOnParts[0].Insert(0, "0");
                    }
                    if (endOnParts[1].Length < 2)
                    {
                        endOnParts[1] = endOnParts[1].Insert(0, "0");
                    }
                    var newEndOn = endOnParts[2]+endOnParts[0]+endOnParts[1];
                    //output returns the previous day
                    //int newEndOnNum = Int32.Parse(newEndOn);
                    //newEndOnNum = newEndOnNum + 1;

                    if (yrtr=="YearAY")
                    {
                        continue;
                    }
                    if(endDate <= DateTime.Now)
                    {
                        continue;
                    }
                    else
                    {
                        string newLine = $"{yrtr.Trim()},{acad.title.Trim()},{acad.title.Trim()},{newStartOn.Trim()},{newEndOn.Trim()}";                         
                        await writer.WriteLineAsync(newLine);
                        _logger.LogString($"recordLine: {newLine}:", LogEntryType.Info);
                    }
                }
                Console.WriteLine("Calendar.csv finished writing");
            }
        }
    }
}
