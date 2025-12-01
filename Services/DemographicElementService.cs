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

namespace OaklandCC.Ethos.eLumen.Services
{
    public class DemographicElementService
    {
        private readonly EthosClient _ethosClient;
        private readonly IConfiguration _configuration;
        private readonly Logger _logger;

        /// <summary>  
        /// Initializes a new instance of the <see cref="DemographicElementService"/> class.  
        /// </summary>  
        /// <param name="ethosClient">The Ethos client for API interactions.</param>  
        /// <param name="configuration">The application configuration settings.</param>  
        /// <param name="logger">The logger for logging messages.</param>  
        public DemographicElementService(EthosClient ethosClient, IConfiguration configuration, Logger logger)
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
        public async Task CreateDemoelementFile()
        {
            var miscService = new MiscService(_ethosClient);

            // Retrieve file path from appsettings.json  
            string? filePath = _configuration["FileSettings:DemoelemOutputFilePath"];
            if (string.IsNullOrEmpty(filePath))
            {
                _logger.LogString("Error: File path is not configured in appsettings.json.", LogEntryType.Error);
                return;
            }
            _logger.LogString($"CampusOutputFilePath: {filePath}:", LogEntryType.Info);

            // Retrieve sites  
            List<MiscText> elementList = await miscService.GetAllTextAsync(limit: 500);

            using (StreamWriter writer = new StreamWriter(filePath, append: false))
            {
                await writer.WriteLineAsync("CATEGORY,ELEMENTSEQ,VALUE");
                Console.WriteLine("Writing DemographicElement.csv");
                // Process each location  
                foreach (var element in elementList)
                    if (element.id == "misc.text-2belumen_demoelement")
                    {
                        {
                            var input = element.text;

                            // Predefined categories you expect to find
                            string[] categories = new[] { "Gender", "Race Ethnicity", "Age Category", "Residency Status" };

                            foreach (string category in categories)
                            {
                                // Use regex to match patterns like: Gender|1|Male
                                var pattern = $@"{Regex.Escape(category)}\|(\d+)\|(.+?)(?=(?:\s*{string.Join("|", categories.Select(Regex.Escape))}\|)|$)";
                                var matches = Regex.Matches(input, pattern);

                                foreach (Match match in matches)
                                {
                                    var elem = match.Groups[1].Value.Trim();
                                    var value = match.Groups[2].Value.Trim();

                                    string recordLine = $"{category},{elem},{value}";
                                    await writer.WriteLineAsync(recordLine);
                                    _logger.LogString($"recordLine: {recordLine}", LogEntryType.Info);
                                }
                            }
                        }
                    }
                Console.WriteLine("DemographicElement.csv finished writing");
                }
            }
        }
    }

