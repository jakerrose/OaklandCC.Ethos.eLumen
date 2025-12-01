using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using OaklandCC.Ethos.Common;
using OaklandCC.Ethos.Common.Models;
using OaklandCC.Ethos.Common.Services;

namespace OaklandCC.Ethos.eLumen.Services
{
    public class CouTypeService
    {
        private readonly EthosClient _ethosClient;
        private readonly IConfiguration _configuration;
        private readonly Logger _logger;

        /// <summary>  
        /// Initializes a new instance of the <see cref="CouTypeService"/> class.  
        /// </summary>  
        /// <param name="ethosClient">The Ethos client for API interactions.</param> 
        /// <param name="configuration">The application configuration settings.</param>  
        /// <param name="logger">The logger for logging messages.</param>  
        public CouTypeService(EthosClient ethosClient, IConfiguration configuration, Logger logger)
        {
            _ethosClient = ethosClient;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>  
        /// Creates a COUTYPE file 
        /// </summary>  
        /// <returns>A task representing the asynchronous operation.</returns>  
        /// <remarks>  
        ///   
        /// </remarks>  
        public async Task CreateCOUTYPEFile()
        {
            var coutypeService = new InstructionalMethodService(_ethosClient);

            // Retrieve file path from appsettings.json  
            string? filePath = _configuration["FileSettings:CouTypeFilePath"];
            if (string.IsNullOrEmpty(filePath))
            {
                _logger.LogString("Error: File path is not configured in appsettings.json.", LogEntryType.Error);
                return;
            }
            _logger.LogString($"COUTYPEFilePath: {filePath}:", LogEntryType.Info);

            // Retrieve COUTYPEs  
            List<InstructionalMethods> COUTYPEList = await coutypeService.GetAllMethodsAsync();

            using (StreamWriter writer = new StreamWriter(filePath, append: false))
            {
                // Write header line  
                await writer.WriteLineAsync("COUTYPEID,NAME,DS");
                Console.WriteLine("Writing CouType.csv");
                // Process each COUTYPEID  
                foreach (var COUTYPE in COUTYPEList)
                {
                    string? ds = null;
                    //Write the record line  
                   string recordLine = $"{COUTYPE.abbreviation.Trim()},{COUTYPE.title.Trim()},{ds}";
                   await writer.WriteLineAsync(recordLine);
                   _logger.LogString($"recordLine: {recordLine}:", LogEntryType.Info);
                }
                Console.WriteLine("CouType.csv finished writing");
            }
        }
    }
}
