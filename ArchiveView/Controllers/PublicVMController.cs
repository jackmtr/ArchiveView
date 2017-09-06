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
        [HttpGet]
        public ActionResult Index([Bind(Prefix = "folderId")] string Folder_ID, string navBarGroup = null, string navBarItem = null, string filter = null, string searchTerm = null, string IssueYearMinRange = "", string IssueYearMaxRange = "", bool Admin = false, string IssueMonthMinRange = "", string IssueMonthMaxRange = "")
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

            //DateTime issueDateMin = today.AddYears(-1); //appropriate place?
            //DateTime issueDateMax = today; //appropriate place?

            DateTime issueDateMin = ((string)TempData["Role"] == "Admin") ? today.AddYears(-30) : today.AddYears(-1);
            //DateTime issueDateMax = today.AddDays(30);
            //DateTime issueDateMax = today;
            DateTime? issueDateMax = null;

            if (searchTerm != null) {
                searchTerm = searchTerm.Trim();
            }

            //declare and instantiate the original full PublicVM data for the client
            IEnumerable<PublicVM> publicModel = null;


            //**POPULATING MAIN MODEL, second conditional is for no doc reference documents, a unique westland condition
            try
            {
                publicModel = publicRepository
                                .SelectAll(Folder_ID, TempData["Role"].ToString())
                                    .Where(n => n.EffectiveDate != null || n.EffectiveDate == null && n.RefNumber == null || n.EffectiveDate == null && n.RefNumber != null);
                //right now, publicModel is full of units of DocReference x DocId
            }
            catch
            {
                TempData.Clear();
                TempData["error_info"] = "The client does not have any available records.";
                TempData["importance"] = false;

                return RedirectToAction("Index", "ErrorHandler", null);
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

                //maybe should return view here already since overall count is alreadt 0
                if (publicModel.Count() == 0)
                {
                    TempData["error_info"] = "The client does not have any available records.";
                    TempData["importance"] = true;

                    return RedirectToAction("Index", "ErrorHandler", null);
                }

                //creating the options for the dropdown list
                TempData["YearRange"] = YearRangePopulate(RetrieveYear(publicModel, true), RetrieveYear(publicModel, false));

                //**Populating the navbar, put into function
                populateNavBar(publicModel);

                //pretty much should only be the initial synchronous load to come in here
                ViewData["SortOrder"] = sortAscending;
                publicModel = publicModel
                                .OrderByDescending(r => r.IssueDate)
                                    //.Where(r => (r.IssueDate >= issueDateMin) && (r.IssueDate <= issueDateMax))
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
                    //if ((string)TempData["Role"] == "Admin") {
                    //    IssueYearMinRange = "1975";
                    //}
                    //entered in two scenarios: 1) regular minIssueDate input and custom date where only minIssueDate is filled
                    int yearInput = (string.IsNullOrEmpty(IssueYearMinRange)) ? Int32.Parse(today.AddYears(-1).Year.ToString()) : Int32.Parse(IssueYearMinRange);
                    issueDateMin = (yearInput > 0) ? issueDateMin = new DateTime(yearInput, 1, 1) : issueDateMin = today.AddYears(yearInput);

                    yearInput = (yearInput > 0) ? yearInput - DateTime.Now.Year : yearInput;
                    // bug, sometimes issueDateMin should be relative to full year, not to today
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
                    //JACKIE
                    //checks if the date filter and search term will return any results
                    //The admin search will also search for document Id within the same input (so checks tbl_Document.Description and tbl_Document.Document_Id)
                    if (!publicModel.Any(r => (r.IssueDate >= issueDateMin) && (issueDateMax == null || r.IssueDate <= issueDateMax) && (searchTerm == null || r.Description.ToLower().Contains(searchTerm.ToLower()) || r.Document_ID.ToString().Contains(searchTerm))))
                    {
                        //ViewData["goodSearch"] = false means no records is found
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
                publicModel = FilterModel(publicModel, filter);

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

        public ActionResult MiscData([Bind(Prefix = "documentId")] string Document_ID, string navBarGroup, string navBarItem) {
            //declare and instantiate the original full MiscPublicData data for the client
            MiscPublicData documentData = null;

            documentData = documentRepository
                                .GetMiscPublicData(Document_ID);

            if (documentData != null)
            {
                ViewData["currentNav"] = navBarGroup;
                ViewData["currentNavTitle"] = navBarItem;

                return PartialView(documentData);
            }
            else {
                return HttpNotFound();
            }
        }

        // Get: File
        public ActionResult FileDisplay([Bind(Prefix = "documentId")] string id)
        {
            tbl_Document file = (User.IsInRole("IT-ops") ? documentRepository.SelectById(id, true) : documentRepository.SelectById(id, false));

            string MimeType = null;

            if (file == null) {
                ViewData["repositoryRequestDocId"] = id;

                TempData.Clear();
                TempData["error_info"] = "The document is not available or does not exist."; //maybe seperate this later with a check for tbl_doc.Active_IND
                TempData["importance"] = false;

                return RedirectToAction("Index", "ErrorHandler", null);
            }

            if (file.ArchivedFile.Length < 100)
            {
                //rare occation of when there is a file, but possibly corrupted

                TempData["error_info"] = "The file was unable to be open.";
                TempData["importance"] = true;

                return RedirectToAction("Index", "ErrorHandler", null);
            }

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
                    break;

                case "ppt":
                    MimeType = "application/vnd.ms-powerpoint";
                    break;

                case "xls":
                case "csv":
                    MimeType = "application/vnd.ms-excel";
                    break;

                case "xlsx":
                    MimeType = "application/vnd.ms-excel.12";
                    break;

                case "doc":
                case "dot":
                    MimeType = "application/msword";
                    break;

                case "docx":
                    MimeType = "application/vnd.ms-word.document.12";
                    break;

                default:
                    MimeType = "text/html";
                    break;
            }

            if (file.ArchivedFile == null) {
                ViewData["repositoryRequestDocId"] = id;

                return PartialView("_FileDisplay");
            }

            return File(file.ArchivedFile, MimeType);
        }

        protected override void Dispose(bool disposing)
        {
            publicRepository.Dispose();
            documentRepository.Dispose();

            base.Dispose(disposing);
        }

        /// <summary>
        /// Method to return a collection of years within two year inputs
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
        /// Method used to find the first or last year within the PublicVM Model
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

                foreach (PublicVM pp in model.GroupBy(g => g.DocumentTypeName).Select(g => g.First()))
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
                                    //check this logic later
                                    .Where(e => e.EffectiveDate != null && e.ReferenceType == "Policy")
                                        .OrderBy(e => e.RefNumber)
                                            .GroupBy(e => e.RefNumber)
                                                .Select(g => g.First().RefNumber);
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

        private IEnumerable<PublicVM> FilterModel(IEnumerable<PublicVM> model, string filter/*, string prevFilter*//*, int page, int pageSize*/)
        {
            if (filter == null || filter == "")
            {
                sortAscending = true;
            }
            else {
                sortAscending = !sortAscending;
            }

            switch (filter)
            {

                case "document":

                    if (sortAscending)
                    {
                        model = model
                                    .OrderBy(r => r.DocumentTypeName)
                                        .ToList();
                    }
                    else
                    {
                        model = model
                                    .OrderByDescending(r => r.DocumentTypeName)
                                        .ToList();
                    }

                    break;

                case "method":

                    if (sortAscending)
                    {
                        model = model
                                    .OrderBy(r => r.Method)
                                        .ToList();
                    }
                    else
                    {
                        model = model
                                    .OrderByDescending(r => r.Method)
                                        .ToList();
                    }

                    break;

                case "policy":

                    if (sortAscending)
                    {
                        model = model
                                    .OrderBy(r => r.RefNumber)
                                        .ToList();
                    }
                    else
                    {
                        model = model
                                    .OrderByDescending(r => r.RefNumber)
                                        .ToList();
                    }

                    break;

                case "effective":

                    if (sortAscending)
                    {
                        model = model
                                    .OrderBy(r => r.EffectiveDate)
                                        .ToList();
                    }
                    else
                    {
                        model = model
                                    .OrderByDescending(r => r.EffectiveDate)
                                        .ToList();
                    }

                    break;

                case "originator":

                    if (sortAscending)
                    {
                        model = model
                                    .OrderBy(r => r.Originator)
                                        .ToList();
                    }
                    else
                    {
                        model = model
                                    .OrderByDescending(r => r.Originator)
                                        .ToList();
                    }

                    break;

                case "reason":

                    if (sortAscending)
                    {
                        model = model
                                    .OrderBy(r => r.Reason)
                                        .ToList();
                    }
                    else
                    {
                        model = model
                                    .OrderByDescending(r => r.Reason)
                                        .ToList();
                    }

                    break;

                case "supplier":

                    if (sortAscending)
                    {
                        model = model
                                    .OrderBy(r => r.Supplier)
                                        .ToList();
                    }
                    else
                    {
                        model = model
                                    .OrderByDescending(r => r.Supplier)
                                        .ToList();
                    }

                    break;

                case "description":

                    if (sortAscending)
                    {
                        model = model
                                    .OrderBy(r => r.Description)
                                        .ToList();
                    }
                    else
                    {
                        model = model
                                    .OrderByDescending(r => r.Description)
                                        .ToList();
                    }

                    break;

                case "file":

                    if (sortAscending)
                    {
                        model = model
                                    .OrderBy(r => r.FileExtension)
                                        .ToList();
                    }
                    else
                    {
                        model = model
                                    .OrderByDescending(r => r.FileExtension)
                                        .ToList();
                    }

                    break;

                case "documentId":

                    if (sortAscending)
                    {
                        model = model
                                    .OrderBy(r => r.Document_ID)
                                        .ToList();
                    }
                    else
                    {
                        model = model
                                    .OrderByDescending(r => r.Document_ID)
                                        .ToList();
                    }

                    break;

                case "hidden":

                    if (sortAscending)
                    {
                        model = model
                                    .OrderBy(r => r.Hidden)
                                        .ToList();
                    }
                    else
                    {
                        model = model
                                    .OrderByDescending(r => r.Hidden)
                                        .ToList();
                    }

                    break;

                default:

                    if (sortAscending)
                    {
                        model = model
                                .OrderByDescending(r => r.IssueDate)
                                    .ToList();
                    }
                    else
                    {
                        model = model
                                .OrderBy(r => r.IssueDate)
                                    .ToList();
                    }

                    break;
            }
            return model;
        }

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
    }
}