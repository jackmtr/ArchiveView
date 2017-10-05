using ArchiveView.Repositories;
using ArchiveView.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Globalization;
using ArchiveView.Models;
using ArchiveView.Filters;
using ArchiveView.Exceptions;
using ArchiveView.CustomHelpers;

namespace ArchiveView.Controllers
{
    public class PublicVMController : Controller
    {
        private IPublicRepository publicRepository = null; //public function repository
        private IDocumentRepository documentRepository = null; //document function repository
        private static bool sortAscending = true; //static var for rememebering previous sort order
        private static DateTime today = DateTime.Today; //should this be static?

        public PublicVMController() {
            //public repo for publicVM actions
            this.publicRepository = new PublicRepository();

            //doc repo for document actions
            this.documentRepository = new DocumentRepository();
        }

        //keep for future testing
        /*
        public PublicVMController(IPublicRepository repository) {

            this.repository = repository;
        }
        */

        // GET: PublicVM
        /// <summary>
        /// The main action for ArchiveView, populates the records and takes the parameters to do any asynch filters
        /// </summary>
        /// <param name="Folder_ID">The Folder Id of the specified Client</param>
        /// <param name="navBarGroup">Optional: The category name of the clicked navBar item</param>
        /// <param name="navBarItem">Optional: The document type name of the clicked navBar item</param>
        /// <param name="sort">Optional: The column clicked on to sort</param>
        /// <param name="searchTerm">Optional: The searchTerm given</param>
        /// <param name="IssueYearMinRange">Optional: The low end year range of IssueDate to filter</param>
        /// <param name="IssueYearMaxRange">Optional: The high end year range of IssueDate to filter</param>
        /// <param name="IssueMonthMinRange">Optional: The low end month range of IssueDate to filter</param>
        /// <param name="IssueMonthMaxRange">Optional: The high end month range of IssueDate to filter</param>
        /// <param name="Admin">Optional: bool to check if Admin (SHOULD NOT BE DONE AS parameter)</param>
        /// <returns>A filtererd on unfiltered collection of PublicVM Objects to be displayed in a the main view</returns>
        [HttpGet]
        public ActionResult Index([Bind(Prefix = "folderId")] string Folder_ID, string navBarGroup = null, string navBarItem = null, string sort = null, string searchTerm = null, string IssueYearMinRange = "", string IssueYearMaxRange = "", string IssueMonthMinRange = "", string IssueMonthMaxRange = "", bool Admin = false)
        {
            //**GLOBAL VARIABLES

            //TempData can be used to send data between controllers and views through one request, .keep() is used to continue keeping after one request
            //persist client name, id, search term, inputted dates
            TempData.Keep("Client_Name");
            TempData.Keep("Client_Id");
            TempData.Keep("Folder_Id");
            TempData.Keep("Role");
            TempData.Keep("RoleButton");
            //***Pseudo save state immitation
            TempData.Keep("SearchTerm");
            TempData.Keep("YearRange"); //will carry an array with allowable issue date years for custom dropdown list

            //ViewData["goodSearch"] = false means seachterm will return an empty result
            ViewData["goodSearch"] = true; //do i still need this var?
            //ViewData["currentNav"] used to populate view's link route value for navBarGroup, which in turn populates navBarGroup variable.  Used to save navBarGroup state
            ViewData["currentNav"] = null;

            //declare and instantiate the original full PublicVM data for the client
            IEnumerable<PublicVM> publicModel = null;

            //Admin should default see all data, while typical user should just see 1 year
            DateTime issueDateMin = ((string)TempData["Role"] == "Admin") ? today.AddYears(-30) : today.AddYears(-1);
            DateTime? issueDateMax = null;

            TempData["Version"] = System.Configuration.ConfigurationManager.AppSettings["Versioning"];

            InMemoryCache cacheprovider = new InMemoryCache();

            //removing whitespaces from end of search term to use before querying
            if (searchTerm != null) {
                searchTerm = searchTerm.Trim();
            }

            //**POPULATING MAIN MODEL, second conditional is for no doc reference documents, a unique westland condition
            try
            {
                //right now, publicModel is full of units of DocReference x DocId
                
                //publicModel = publicRepository
                //                .SelectAll(Folder_ID, TempData["Role"].ToString()) //find a way to cache this initial load
                //                    .Where(n => n.EffectiveDate != null || n.EffectiveDate == null && n.RefNumber == null || n.EffectiveDate == null && n.RefNumber != null);

                //added a caching mechanism where on initial load, the model will be cached for 10 mins.
                //if no cache, it will run the default call to the db
                publicModel = cacheprovider.GetOrSet(
                    Folder_ID,
                    () => publicRepository
                                .SelectAll(Folder_ID, TempData["Role"].ToString()) //find a way to cache this initial load
                                    .Where(n => n.EffectiveDate != null || n.EffectiveDate == null && n.RefNumber == null || n.EffectiveDate == null && n.RefNumber != null)
                    );
            }
            catch
            {
                //the clientId should always exist.  if it doesnt, i would assume the user shouldnt be here, which is why i issue an exception
                NoResultException exception = new NoResultException("The client may not exist or does not have any available documents.");
                exception.HelpLink = "Please check over the provided information and try again.";
                exception.Data["Folder ID"] = Folder_ID;

                throw exception;
            }

            //needs to be checked seperately because cant select by DocId until after deciding if we are searching by documentId or policy
            //can possibly combine into navBarGroupFilter
            if (navBarGroup != "policy")
            {
                publicModel = publicModel.GroupBy(x => x.Document_ID).Select(x => x.First());
            }


            //should only be run on initial load of page
            if (!Request.IsAjaxRequest())
            {
                //count of total records unfiltered of this client
                ViewData["allRecordsCount"] = publicModel.Count();

                //maybe should return view here already since overall count is already 0
                if (publicModel.Count() == 0)
                {
                    //maybe reword exception message
                    NoResultException exception = new NoResultException("The client does not have any available records.");
                    exception.HelpLink = "Please check over the provided information and try again.";
                    exception.Data["Folder ID"] = Folder_ID;

                    throw exception;
                }

                //creating the options for the dropdown list
                TempData["YearRange"] = YearRangePopulate(RetrieveYear(publicModel, true), RetrieveYear(publicModel, false));

                //**Populating the navbar, put into function
                populateNavBar(publicModel);

                //pretty much should only be the initial synchronous load to come in here
                ViewData["SortOrder"] = sortAscending;
                publicModel = publicModel
                                .OrderByDescending(r => r.IssueDate)
                                    .ThenByDescending( r => r.ArchiveTime)
                                        .Where(r => r.IssueDate >= issueDateMin)
                                            .ToList();

                ViewData["currentRecordsCount"] = publicModel.Count();

                return View(publicModel);
            }
            else
            {
                //If user inputs only one custom year and maybe one/two months, what should happen?
                if (String.IsNullOrEmpty(IssueYearMaxRange))
                {
                    //entered in two scenarios: 1) regular minIssueDate input and custom date where only minIssueDate is filled
                    int yearInput = (string.IsNullOrEmpty(IssueYearMinRange)) ? Int32.Parse(today.AddYears(-1).Year.ToString()) : Int32.Parse(IssueYearMinRange);

                    issueDateMin = (yearInput > 0) ? issueDateMin = new DateTime(yearInput, 1, 1) : issueDateMin = today.AddYears(yearInput);

                    yearInput = (yearInput > 0) ? yearInput - DateTime.Now.Year : yearInput;
                }
                else if (!String.IsNullOrEmpty(IssueYearMinRange) && !String.IsNullOrEmpty(IssueYearMaxRange))
                {
                    //custom dates
                    issueDateMin = FormatDate(IssueYearMinRange, IssueMonthMinRange, true);
                    issueDateMax = FormatDate(IssueYearMaxRange, IssueMonthMaxRange, false);
                }
                else
                {
                    //no input for min date under CUSTOM inputs
                    //min would be oldest issue date available
                    IssueMonthMaxRange = (String.IsNullOrEmpty(IssueMonthMaxRange)) ? "12" : IssueMonthMaxRange;
                    issueDateMin = DateTime.ParseExact(String.Format("01/01/1985"), "d", CultureInfo.InvariantCulture);
                    issueDateMax = FormatDate(IssueYearMaxRange, IssueMonthMaxRange, false);
                }

                //**STARTING ACTUAL FILTERING/SORTING OF MODEL**

                //*filtering by category/doctype/policy
                if (navBarGroup != null)
                {
                    publicModel = navBarGroupFilter(publicModel, navBarGroup, navBarItem);
                }

                //*filtering by date and search conditions
                if (TempData["Role"].ToString() == "Admin")
                {
                    //checks if the date filter and search term will return any results
                    //The admin search will also search for document Id within the same input (so checks tbl_Document.Description and tbl_Document.Document_Id)
                    if (!publicModel.Any(r => (r.IssueDate >= issueDateMin) && (issueDateMax == null || r.IssueDate <= issueDateMax) && (searchTerm == null || r.Description.ToLower().Contains(searchTerm.ToLower()) || r.Document_ID.ToString().Contains(searchTerm))))
                    {
                        //ViewData["goodSearch"] equals false means no records is found
                        ViewData["goodSearch"] = false;
                    }
                    else
                    {
                        publicModel = publicModel.Where(r => (r.IssueDate >= issueDateMin) && (issueDateMax == null || r.IssueDate <= issueDateMax) && (searchTerm == null || ((bool)ViewData["goodSearch"] ? r.Description.ToLower().Contains(searchTerm.ToLower()) || r.Document_ID.ToString().Contains(searchTerm) == true : true)));

                        TempData["SearchTerm"] = searchTerm;
                    }
                }
                else
                {
                    //checks if the date filter and search term will return any results
                    publicModel = publicModel.Where(r => (r.IssueDate >= issueDateMin) && (issueDateMax == null || r.IssueDate <= issueDateMax) && (searchTerm == null || ((bool)ViewData["goodSearch"] ? r.Description.ToLower().Contains(searchTerm.ToLower()) == true : true)));
                }

                ViewData["currentRecordsCount"] = publicModel.Count();

                if (publicModel.Count() == 0) //not sure if needed
                {
                    ViewData["goodSearch"] = false;
                }

                TempData["SearchTerm"] = searchTerm; //not sure if needed

                //*sorting data
                publicModel = SortModel(publicModel, sort);

                //**ENDING FILTERING OF MODEL**

                if (publicModel != null)
                {
                    ViewData["SortOrder"] = sortAscending;

                    return PartialView("_PublicTable", publicModel);
                }
                else
                {
                    return HttpNotFound();
                }
            }
        }

        /// <summary>
        /// populates the additional info all records have into a table
        /// </summary>
        /// <param name="Document_ID">The document Id</param>
        /// <param name="navBarGroup">Optional: The category name of the clicked navBar item</param>
        /// <param name="navBarItem">Optional: The document type name of the clicked navBar item</param>
        /// <returns>a partial view table inserted into the main table asynchronously</returns>
        public ActionResult MiscData([Bind(Prefix = "documentId")] string Document_ID, string navBarGroup, string navBarItem) {

            MiscPublicData documentData = null;

            documentData = documentRepository
                                .GetMiscPublicData(Document_ID);

            ViewData["currentNav"] = navBarGroup;
            ViewData["currentNavTitle"] = navBarItem;

            return PartialView(documentData);
        }

        /// <summary>
        /// Show the contents of a file.
        /// </summary>
        /// <param name="id">The document Id of the file in question</param>
        /// <returns>The File's contents in a new page on the browser</returns>
        // Get: File
        [HandleError(ExceptionType = typeof(NoResultException), View = "_Error")]
        public ActionResult FileDisplay([Bind(Prefix = "documentId")] string id)
        {
            string MimeType = null;

            //only admins can see hidden file contents
            tbl_Document file = (User.IsInRole("IT-ops") ? documentRepository.SelectById(id, true) : documentRepository.SelectById(id, false));

            if (file == null)
            {
                NoResultException exception = new NoResultException("The document is not available or does not exist.");
                exception.HelpLink = "Please check over the provided information and try again.";
                exception.Data["Document ID"] = id;

                throw exception;
            }
            else if (file.ArchivedFile == null)
            {
                NullReferenceException exception = new NullReferenceException("The file appears to be missing or corrupt.");
                exception.HelpLink = "Please contact ServiceNow if additional help is needed.";

                throw exception;
            }
            else if (file.ArchivedFile.Length < 100)
            {
                //rare occation of when there is a file, but possibly corrupted
                //better to use IOException, but seems like a waste to bring in a whole new package for this
                Exception exception = new Exception("The file was unable to be open.");
                exception.HelpLink = "Please contact Support at ServiceNow";
                exception.Data["Document ID"] = id;

                throw exception;
            }
            else {
                //maybe can do this better than comparing strings
                switch (file.FileExtension.ToLower().Trim()) {

                    case "pdf":
                        MimeType = "application/pdf";
                        break;

                    case "gif":
                        MimeType = "image/gif";
                        break;

                    case "jpg":
                        MimeType = "image/jpeg";
                        break;

                    case "msg":
                        MimeType = "application/vnd.outlook";
                        //HttpContext.Response.AddHeader("Content-Disposition", "Attachment");
                        break;

                    case "ppt":
                        MimeType = "application/vnd.ms-powerpoint";
                        HttpContext.Response.AddHeader("Content-Disposition", "Attachment");
                        break;

                    case "xls":
                        MimeType = "application/vnd.ms-excel";
                        HttpContext.Response.AddHeader("Content-Disposition", "Attachment");
                        break;
                    case "csv":
                        MimeType = "application/vnd.ms-excel";
                        HttpContext.Response.AddHeader("Content-Disposition", "Attachment");
                        break;

                    case "xlsx":
                        MimeType = "application/vnd.ms-excel.12";
                        HttpContext.Response.AddHeader("Content-Disposition", "Attachment");
                        break;

                    case "doc":
                    case "dot":
                        MimeType = "application/msword";
                        HttpContext.Response.AddHeader("Content-Disposition", "Attachment");
                        break;

                    case "docx":
                        MimeType = "application/vnd.ms-word.document.12";
                        HttpContext.Response.AddHeader("Content-Disposition", "Attachment");
                        break;

                    default:
                        MimeType = "text/html";
                        break;
                }
            }
            return File(file.ArchivedFile, MimeType);
        }

        /// <summary>
        /// Return a collection of years within two year inputs
        /// </summary>
        /// <param name="IssueYearMinRange">DateTime of model's issue date lower range</param>
        /// <param name="IssueYearMaxRange">DateTime of model's issue date upper range</param>
        /// <returns>An IList Collection of years within the two parameter's years</returns>
        private IList<SelectListItem> YearRangePopulate(DateTime IssueYearMinRange, DateTime IssueYearMaxRange) {

            IList<SelectListItem> years = new List<SelectListItem>();

            for (int i = IssueYearMinRange.Year; i <= IssueYearMaxRange.Year; i++) {

                SelectListItem year = new SelectListItem();
                year.Text = year.Value = i.ToString();
                years.Add(year);
            }

            return years;
        }

        /// <summary>
        /// Method used to find an outer limit issue date year within the PublicVM Model
        /// </summary>
        /// <param name="model">PublicVM model that will be used, should use before any filters</param>
        /// <param name="ascending">True means it will find the year of the lowest issue date, vice versa for False</param>
        /// <returns>DateTime of an outer range of the model's IssueDate column</returns>
        private DateTime RetrieveYear(IEnumerable<PublicVM> model, bool ascending) {

            DateTime date;

            if (ascending) {
                date = model
                        .Where(y => y.IssueDate != null)
                            .OrderBy(r => r.IssueDate)
                                .First()
                                    .IssueDate.Value;
            }
            else
            {
                date = model
                        .Where(y => y.IssueDate != null)
                            .OrderByDescending(r => r.IssueDate)
                                .First()
                                    .IssueDate.Value;
            }

            return date;
        }

        /// <summary>
        /// Method that generates the list items to bring to view for NavBar
        /// </summary>
        /// <param name="model">The publicVM Model used</param>
        private void populateNavBar(IEnumerable<PublicVM> model)
        {
            IEnumerable<PublicVM> nb = model
                                        .OrderBy(e => e.CategoryName)
                                            .GroupBy(e => e.CategoryName)
                                                .Select(g => g.First());

            List<NavBar> nbl = new List<NavBar>();

            foreach (PublicVM pvm in nb)
            {
                NavBar nbitem = new NavBar();

                nbitem.CategoryName = pvm.CategoryName;

                foreach (PublicVM pp in model.GroupBy(g => g.DocumentTypeName).Select(g => g.First()).OrderBy(e => e.DocumentTypeName))
                {
                    if (pp.CategoryName == nbitem.CategoryName && !nbl.Any(s => s.DocumentTypeName.Contains(pp.DocumentTypeName)))
                    {
                        nbitem.DocumentTypeName.Add(pp.DocumentTypeName);
                    }
                }

                nbl.Add(nbitem);
            }

            ViewBag.CategoryNavBar = nbl;

            ViewBag.PolicyNavBar = model
                        .Where(e => e.EffectiveDate != null && e.ReferenceType == "Policy")
                            .OrderBy(e => e.RefNumber)
                                .GroupBy(e => e.RefNumber)
                                    .Select(g => g.First().RefNumber).Where(y => y != "");
        }

        /// <summary>
        /// Method to filter the current publicVM model with navbar criteria
        /// </summary>
        /// <param name="model">The current PublicVM Model</param>
        /// <param name="navBarGroup"></param>
        /// <param name="navBarItem"></param>
        /// <returns></returns>
        private IEnumerable<PublicVM> navBarGroupFilter(IEnumerable<PublicVM> model, string navBarGroup, string navBarItem)
        {
            switch (navBarGroup)
            {
                case "category":
                    model = model.Where(r => r.CategoryName == navBarItem);
                    break;
                case "doctype":
                    model = model.Where(r => r.DocumentTypeName == navBarItem);
                    break;
                case "policy":
                    model = model.Where(r => r.RefNumber == navBarItem);
                    break;
            }

            ViewData["currentNav"] = navBarGroup;
            ViewData["currentNavTitle"] = navBarItem;

            return model;
        }

        /// <summary>
        /// Sort the model
        /// </summary>
        /// <param name="model">The current publicVM model</param>
        /// <param name="sort"></param>
        /// <returns></returns>
        private IEnumerable<PublicVM> SortModel(IEnumerable<PublicVM> model, string sort)
        {
            if (sort == null || sort == "")
            {
                sortAscending = true;
            }
            else {
                sortAscending = !sortAscending;
            }

            switch (sort)
            {
                case "document":
                    if (sortAscending)
                    {
                        model = model
                                    .OrderBy(r => r.DocumentTypeName)
                                        .ThenByDescending(r => r.ArchiveTime)
                                            .ToList();
                    }
                    else
                    {
                        model = model
                                    .OrderByDescending(r => r.DocumentTypeName)
                                        .ThenByDescending(r => r.ArchiveTime)
                                            .ToList();
                    }
                    break;

                case "method":
                    if (sortAscending)
                    {
                        model = model
                                    .OrderBy(r => r.Method)
                                        .ThenByDescending(r => r.ArchiveTime)
                                            .ToList();
                    }
                    else
                    {
                        model = model
                                    .OrderByDescending(r => r.Method)
                                        .ThenByDescending(r => r.ArchiveTime)
                                            .ToList();
                    }
                    break;

                case "policy":
                    if (sortAscending)
                    {
                        model = model
                                    .OrderBy(r => r.RefNumber)
                                        .ThenByDescending(r => r.ArchiveTime)
                                            .ToList();
                    }
                    else
                    {
                        model = model
                                    .OrderByDescending(r => r.RefNumber)
                                        .ThenByDescending(r => r.ArchiveTime)
                                            .ToList();
                    }
                    break;

                case "effective":
                    if (sortAscending)
                    {
                        model = model
                                    .OrderBy(r => r.EffectiveDate)
                                        .ThenByDescending(r => r.ArchiveTime)
                                            .ToList();
                    }
                    else
                    {
                        model = model
                                    .OrderByDescending(r => r.EffectiveDate)
                                        .ThenByDescending(r => r.ArchiveTime)
                                            .ToList();
                    }
                    break;

                case "originator":
                    if (sortAscending)
                    {
                        model = model
                                    .OrderBy(r => r.Originator)
                                        .ThenByDescending(r => r.ArchiveTime)
                                            .ToList();
                    }
                    else
                    {
                        model = model
                                    .OrderByDescending(r => r.Originator)
                                        .ThenByDescending(r => r.ArchiveTime)
                                            .ToList();
                    }
                    break;

                case "reason":
                    if (sortAscending)
                    {
                        model = model
                                    .OrderBy(r => r.Reason)
                                        .ThenByDescending(r => r.ArchiveTime)
                                            .ToList();
                    }
                    else
                    {
                        model = model
                                    .OrderByDescending(r => r.Reason)
                                        .ThenByDescending(r => r.ArchiveTime)
                                            .ToList();
                    }
                    break;

                case "supplier":
                    if (sortAscending)
                    {
                        model = model
                                    .OrderBy(r => r.Supplier)
                                        .ThenByDescending(r => r.ArchiveTime)
                                            .ToList();
                    }
                    else
                    {
                        model = model
                                    .OrderByDescending(r => r.Supplier)
                                        .ThenByDescending(r => r.ArchiveTime)
                                            .ToList();
                    }
                    break;

                case "description":
                    if (sortAscending)
                    {
                        model = model
                                    .OrderBy(r => r.Description)
                                        .ThenByDescending(r => r.ArchiveTime)
                                            .ToList();
                    }
                    else
                    {
                        model = model
                                    .OrderByDescending(r => r.Description)
                                        .ThenByDescending(r => r.ArchiveTime)
                                            .ToList();
                    }
                    break;

                case "file":
                    if (sortAscending)
                    {
                        model = model
                                    .OrderBy(r => r.FileExtension)
                                        .ThenByDescending(r => r.ArchiveTime)
                                            .ToList();
                    }
                    else
                    {
                        model = model
                                    .OrderByDescending(r => r.FileExtension)
                                        .ThenByDescending(r => r.ArchiveTime)
                                            .ToList();
                    }
                    break;

                case "documentId":
                    if (sortAscending)
                    {
                        model = model
                                    .OrderBy(r => r.Document_ID)
                                        .ThenByDescending(r => r.ArchiveTime)
                                            .ToList();
                    }
                    else
                    {
                        model = model
                                    .OrderByDescending(r => r.Document_ID)
                                        .ThenByDescending(r => r.ArchiveTime)
                                            .ToList();
                    }
                    break;

                case "hidden":
                    if (sortAscending)
                    {
                        model = model
                                    .OrderBy(r => r.Hidden)
                                        .ThenByDescending(r => r.ArchiveTime)
                                            .ToList();
                    }
                    else
                    {
                        model = model
                                    .OrderByDescending(r => r.Hidden)
                                        .ThenByDescending(r => r.ArchiveTime)
                                            .ToList();
                    }
                    break;

                case "dol":
                    if (sortAscending)
                    {
                        model = model
                                    .OrderBy(r => r.DateOfLoss)
                                        .ThenByDescending(r => r.ArchiveTime)
                                            .ToList();
                    }
                    else
                    {
                        model = model
                                    .OrderByDescending(r => r.DateOfLoss)
                                        .ThenByDescending(r => r.ArchiveTime)
                                            .ToList();
                    }
                    break;

                case "claim":
                    if (sortAscending)
                    {
                        model = model
                                    .OrderBy(r => r.ClaimNumber)
                                        .ThenByDescending(r => r.ArchiveTime)
                                            .ToList();
                    }
                    else
                    {
                        model = model
                                    .OrderByDescending(r => r.ClaimNumber)
                                        .ThenByDescending(r => r.ArchiveTime)
                                            .ToList();
                    }
                    break;

                default:
                    if (sortAscending)
                    {
                        model = model
                                .OrderByDescending(r => r.IssueDate)
                                    .ThenByDescending(r => r.ArchiveTime)
                                        .ToList();
                    }
                    else
                    {
                        model = model
                                .OrderBy(r => r.IssueDate)
                                    .ThenByDescending(r => r.ArchiveTime)
                                        .ToList();
                    }
                    break;
            }
            return model;
        }

        /// <summary>
        /// Formats the date from a year/month scernario
        /// </summary>
        /// <param name="inputYear">The inputted year</param>
        /// <param name="inputMonth">The inputted month</param>
        /// <param name="isStartingDate">bool to check if Start or End Range</param>
        /// <returns>DateTime of the decided year/month combination</returns>
        private DateTime FormatDate(string inputYear, string inputMonth, bool isStartingDate) {

            int year = Int32.Parse(inputYear);
            int month = 0;

            if (inputMonth != "")
            {
                month = Int32.Parse(inputMonth) + 1;
            }
            else {
                if (isStartingDate)
                {
                    month = 2;
                }
                else {
                    month = 13;
                }
            }

            if (month == 13)
            {
                year = Int32.Parse(inputYear) + 1;
                month = 1;
            }

            if (isStartingDate)
            {
                return new DateTime(year, month, 1).AddMonths(-1);
            }
            else {
                DateTime endingDate = new DateTime(year, month, 1).AddDays(-1);

                return endingDate;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            publicRepository.Dispose();
            documentRepository.Dispose();

            base.Dispose(disposing);
        }
    }
}