using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using OaklandCC.Ethos.Common.Models;
using OaklandCC.Ethos.Common.Services;
using OaklandCC.Ethos.eLumen.Services;

namespace OaklandCC.Ethos.eLumen
{
    /// <summary>
    /// The main application class that handles the creation of the campus file.
    /// </summary>
    public class Application : IHostedService
    {   private readonly InitService _initService;
        private readonly InitService2 _initService2;
        //private readonly CampusService _campusService;
        //private readonly CalendarService _calendarService;
        //private readonly DemographicElementService _demographicElementService;
        //private readonly OrgService _orgService;
        //private readonly DemographicCategoryService _demographicCategoryService;
        //private readonly Combine4Services _combine4Service;
        //private readonly OfferingService _offeringservice;
        //private readonly FacAssService _facAssService;
        //private readonly OfferingSecattrService _offeringSecattrService;
        private readonly EnrollmentService _enrollmentService;
        //private readonly CouTypeService _coutypeservice;
        //private readonly SecattrService _secattrService;
        //private readonly ElumenCourseService _elumencourseService;
        //private readonly PeopleService _peopleService;

        private readonly TermOptions _termOptions;
        private readonly Logger _logger;


        public Application(InitService initService, InitService2 initService2, /*CalendarService calendarService, CampusService campusService, DemographicElementService demographicElementService,
            DemographicCategoryService demographicCategoryService, OrgService orgService, FacAssService facAssService,
            OfferingService offeringService, CouTypeService couTypeService,*/ EnrollmentService enrollmentService, /*Combine4Services combine4Service,
            SecattrService secattrService, ElumenCourseService elumencourseService, PeopleService peopleService, OfferingSecattrService offeringSecattrService,*/ Logger logger, TermOptions termOptions) 
        {
            _initService = initService;
            _initService2 = initService2;
            //_campusService = campusService;
            //_calendarService = calendarService;
            //_demographicElementService = demographicElementService;
            //_demographicCategoryService = demographicCategoryService;
            //_orgService = orgService;
            //_combine4Service = combine4Service;
            //_offeringservice = offeringService;
            //_facAssService = facAssService;
            //_offeringSecattrService = offeringSecattrService;
            _enrollmentService = enrollmentService;
            //_coutypeservice = couTypeService;
            //_peopleService = peopleService;
            //_elumencourseService = elumencourseService;
            //_secattrService = secattrService;

            _termOptions = termOptions;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var timeStarted = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            _logger.LogString("Creating Files..." + timeStarted, LogEntryType.Info);

            try
            {
                await _initService.CreateInitFiles();

                //await _campusService.CreateCampusFile();
                //await _demographicElementService.CreateDemoelementFile();
                //await _orgService.CreateOrgFile();
                //await _elumencourseService.CreateCourseFile();
                //await _coutypeservice.CreateCOUTYPEFile();
                //await _calendarService.CreateCalendarFile();
                //await _demographicCategoryService.CreateDemoCategoryFile();
                //await _secattrService.CreateSecattrFile();

                //for(int i=0; i< _initService.SelectedTerms.Count;i++)                   
                //    {   
                //    var term = _initService.SelectedTerms[i];
                //    await _initService2.CreateInit2Files(term);
                //    //await _combine4Service.Create4Files();
                //    //await _facAssService.CreateFacAssFile();
                //    //await _offeringservice.CreateOfferingFile();              
                //    //await _enrollmentService.CreateEnrollmentFile();
                //    //await _offeringSecattrService.CreateOfferingSecattrFile();
                //    await _peopleService.CreatePersonFile();
                //}
                //to run from cli

                for (int i = 0; i < _termOptions.SelectedTerms.Count; i++)
                {                    
                    var term = _termOptions.SelectedTerms[i];
                    await _initService2.CreateInit2Files(term);
                    Console.WriteLine("Starting loop number " + (i+1) + " of " + _termOptions.SelectedTerms.Count + " for term " + term);
                    //await _combine4Service.Create4Files();
                    //await _facAssService.CreateFacAssFile();
                    //await _offeringservice.CreateOfferingFile();
                    await _enrollmentService.CreateEnrollmentFile();
                    //await _offeringSecattrService.CreateOfferingSecattrFile();
                    //await _peopleService.CreatePersonFile();
                }
                Console.WriteLine("Program complete");
                    var timeFinished = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                _logger.LogString("Finished creating files..." + timeFinished, LogEntryType.Info);

                var totalTime = DateTime.Parse(timeFinished) - DateTime.Parse(timeStarted);
                _logger.LogString("Total time: " + totalTime, LogEntryType.Info);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while creating files: {ex.Message}");
                _logger.LogString($"Error while creating files: {ex.Message}", LogEntryType.Error);
            }

            _logger.LogString("--------------------------------------------------", LogEntryType.Info);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogString("Stopping application...", LogEntryType.Info);
            return Task.CompletedTask;
        }
    }
}