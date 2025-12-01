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
    public class SecattrService
    {
        private readonly EthosClient _ethosClient;
        private readonly IConfiguration _configuration;
        private readonly Logger _logger;

        /// <summary>  
        /// Initializes a new instance of the <see cref="SecattrService"/> class.  
        /// </summary>  
        /// <param name="ethosClient">The Ethos client for API interactions.</param>  
        /// <param name="configuration">The application configuration settings.</param>  
        /// <param name="logger">The logger for logging messages.</param>  
        public SecattrService(EthosClient ethosClient, IConfiguration configuration, Logger logger)
        {
            _ethosClient = ethosClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task CreateSecattrFile()
        {
            var miscService = new MiscService(_ethosClient);

            // Retrieve file path from appsettings.json  
            string? filePath = _configuration["FileSettings:SecattrOutputFilePath"];
            if (string.IsNullOrEmpty(filePath))
            {
                _logger.LogString("Error: File path is not configured in appsettings.json.", LogEntryType.Error);
                return;
            }
            _logger.LogString($"SecattrOutputFilePath: {filePath}:", LogEntryType.Info);

            // Retrieve sites  
            List<MiscText> elementList = await miscService.GetAllTextAsync(limit: 500);

            using (StreamWriter writer = new StreamWriter(filePath, append: false))
            {
                await writer.WriteLineAsync("SECATTRID,NAME,DS");

                // Process each location  
                foreach (var element in elementList)
                    if (element.id == "misc.text-2belumen_secattr")
                    {
                        {
                            var input = element.text;
                            Console.WriteLine(input);

                            
                            string[] categories = new[] { "Afternoon","On-Ground","Evening","Hybrid","Morning","Not Applicable", "Weekend",
                            "Online","Online-Asynchronous","Online-Synchronous","AH","HL","OR","RO","SF","No Campus","Online-Live","Online Course Testing in-person",
                            "In-Person","Flexible Live","Flexible Online"};

                            //foreach (string category in categories)
                            //{
                            // Use regex to match patterns like: Gender|1|Male
                            //var pattern = $@"{Regex.Escape(category)}\|(\w+)\|(.+?)(?=(?:\s*{string.Join("|", categories.Select(Regex.Escape))}\|)|$)";
                            var categoryGroup = string.Join("|", categories.Select(Regex.Escape));
                            var pattern = $@"\b({categoryGroup})\|([^\|]+)\|(.+?)(?=(?:\s+(?:{categoryGroup})\|)|$)";
                            var matches = Regex.Matches(input, pattern);

                                foreach (Match match in matches)
                                {
                                var category = match.Groups[1].Value.Trim();
                                var elem = match.Groups[2].Value.Trim();
                                    var value = match.Groups[3].Value.Trim();

                                    string recordLine = $"{category},{elem},{value}";
                                    await writer.WriteLineAsync(recordLine);
                                    _logger.LogString($"recordLine: {recordLine}", LogEntryType.Info);
                                }
                            //}
                        }
                    }
                    else
                    {
                        continue;
                    }
            }
        }
    }
}

