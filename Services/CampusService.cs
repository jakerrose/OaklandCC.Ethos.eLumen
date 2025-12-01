using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
    public class CampusService
    {
        private readonly EthosClient _ethosClient;
        private readonly IConfiguration _configuration;
        private readonly Logger _logger;

        /// <summary>  
        /// Initializes a new instance of the <see cref="CampusService"/> class.  
        /// </summary>  
        /// <param name="ethosClient">The Ethos client for API interactions.</param>  
        /// <param name="configuration">The application configuration settings.</param>  
        /// <param name="logger">The logger for logging messages.</param>  
        public CampusService(EthosClient ethosClient, IConfiguration configuration, Logger logger)
        {
            _ethosClient = ethosClient;
            _configuration = configuration;
            _logger = logger;
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
        public async Task CreateCampusFile()
        {
            var sitesService = new SitesService(_ethosClient);

            // Retrieve file path from appsettings.json  
            string? filePath = _configuration["FileSettings:CampusOutputFilePath"];
            if (string.IsNullOrEmpty(filePath))
            {
                _logger.LogString("Error: File path is not configured in appsettings.json.", LogEntryType.Error);
                return;
            }
            _logger.LogString($"CampusOutputFilePath: {filePath}:", LogEntryType.Info);

            // Retrieve sites  
            List<SiteLocation> locationList = await sitesService.GetAllSitesAsync(limit: 500);

            using (StreamWriter writer = new StreamWriter(filePath, append: false))
            {
                // Write header line  
                await writer.WriteLineAsync("CAMPUSID,NAME,DS");
                Console.WriteLine("Writing Campus.csv");
                // Process each location  
                foreach (var location in locationList)
                {
                    string address = null;
                    if (location.addressLines != null)
                    {
                        address = location.addressLines.Trim();
                    }
                    // Write the record line  
                    string recordLine = $"{location.code.Trim()},{location.title.Trim()},{address}";
                    await writer.WriteLineAsync(recordLine);
                    _logger.LogString($"recordLine: {recordLine}:", LogEntryType.Info);
                }
                Console.WriteLine("Campus.csv finished writing");
            }
        }
    }
}
