﻿// some functions need to be outside of the .ready functionality because they are accessed globally

// ***global functions

//this is a javascript function
//function to clear table/navbar/form to look like original content
function clearFields(id) {
    var $form = $('form');

    $form
        .find("input[type=search]")
            .val("")
        //.end()
        //.find(".form-inputs select option")
        //    .removeAttr('selected');

    if (id != "allDocs") {
        $form
            .find("#IssueYearMinRange option:eq(0)")
                .prop("selected", true)
            .end()
            .find("#IssueYearMaxRange option:eq(0)")
                .prop("selected", true);
    } else {
        $form
            .find("#IssueYearMinRange option:eq(1)")
                .prop("selected", true)
            .end()
            .find("#IssueYearMaxRange option:last-child")
                .prop("selected", true);
    }

    if (id == "clearButton") {
        if ($('#downloadForm')[0]) {
            $form
                .find("#fromYear option:eq(3)")
                    .prop("selected", true);
        } else {
            $form
                .find("#fromYear option:eq(0)")
                    .prop("selected", true);
        }

        $form
            .find("#fromYear")
                .prop("disabled", false);

        $(".customDates select").prop("disabled", true);

        $(".customDates *").css("display", "none");

        $(".active").removeClass('active');

        if ($('#customDates').is(':checked')) {
            $('#customDates').prop('checked', false);
        }
    }

    $('#searchInputBox').focus();
}


// to create a jQuery function, you basically just extend the jQuery prototype
// (using the fn alias)
var postNavbar = function ()
{
    var className = $('.active').children("a")
                                    .data("subclass"),
    classNameTitle = $('.active').children("a")
                                    .data("subclass-title");

    //first if statement checks if $className has a value or is undefined
    if (className) {
        if (classNameTitle == "Correspondence")
        {
            $('#public_table')
                .removeClass()
                .addClass("correspondence");
        } else if (classNameTitle == "Claim Document")
        {
            $('#public_table')
                .removeClass()
                .addClass("claimDocument");
        } else if (classNameTitle == "Declaration/Endorsement")
        {
            $('#public_table')
                .removeClass()
                .addClass("declaration");
        } else
        {
            $('#public_table')
                .removeClass()
                .addClass(className);
        }
    }

    //need to check for form submit, then not do clearFields
    if ($(this).hasClass('navLink') || $(this).hasClass('button'))
    {
        clearFields();
    }

    updateCurrentCount();
}

function rememeberSort($this, ascending)
{
    if (ascending == true)
    {
        $("#" + $this.children('i').attr('id'))
            .removeClass("fa-sort")
            .addClass("fa-sort-desc")
        .parent('A')
            .css('text-decoration', 'underline');
    } else
    {
        $("#" + $this.children('i').attr('id'))
            .removeClass("fa-sort")
            .addClass("fa-sort-asc")
        .parent('A')
            .css('text-decoration', 'underline');
    }
}

function updateCurrentCount()
{
    var currentCount = $('#public_table').data('currentrecord'),
        currentLowYear = $("select[name = 'IssueYearMinRange']:not(:disabled)").val(),
        currentHighYear = $("select[name = 'IssueYearMaxRange']:not(:disabled)").val(),
        searchTerm = $('#searchInputBox').val();

    if (searchTerm.length > 0)
    {
        $('#currentSearchTerm').text(', with term "' + searchTerm + '"');
    } else
    {
        $('#currentSearchTerm').text("");
    }

    if ($("select[name = 'IssueYearMaxRange']:not(:disabled)")[0])
    { //confirms in custom range
        if (currentLowYear == "")
        {
            currentLowYear = $("select[name = 'IssueYearMinRange']:not(:disabled) option:nth-child(2)").text();
        }

        if ($("select[name = 'IssueYearMaxRange']:not(:disabled)").val() != "")
        {
            currentHighYear = $("select[name = 'IssueYearMaxRange']:not(:disabled)").val();
        }
    }

    if (currentLowYear == -100)
    {
        currentLowYear = $("select[name = 'IssueYearMinRange']:disabled option:nth-child(2)").text();
    }
    else if (currentLowYear < 0)
    {
        currentLowYear = (new Date().getFullYear() + parseInt(currentLowYear)); //yearInput would be a negative value
    }

    if (currentLowYear > 0)
    {//takes user input
        $('#currentLowYear').text(currentLowYear);
    } else
    {//defaults back to original low
        currentLowYear = $('#IssueYearMinRange option:eq(1)').val();

        $('#currentLowYear').text(currentLowYear);
    }

    if (currentHighYear > 0)
    {
        $('#currentHighYear').text(currentHighYear);
    } else
    {
        currentHighYear = $('#IssueYearMaxRange option:last').val();

        $('#currentHighYear').text(currentHighYear);
    }

    $('#currentCount').text(currentCount);
}

//only runs when html is ready
$(function () {

    //**GLOBAL VARIABLES
    var editList = [],
        IssueYearMinRange = 1;

    //these validators are used for admin editing checks
    $.validator.addMethod(
        "regex",
        function (value, element, regexp)
        {
            var re = new RegExp(regexp);
            return this.optional(element) || re.test(value);
        },
        "Please check your input."
    );

    $.validator.addMethod('daterange', function (value, element, arg)
    {
        if (this.optional(element) && !value)
        {
            return true;
        }

        var startDate = Date.parse(arg[0]),
            endDate = Date.parse(arg[1]),
            enteredDate = Date.parse(value);

        if (isNaN(enteredDate))
        {
            return false;
        }

        return ((isNaN(startDate) || (startDate <= enteredDate)) &&
                 (isNaN(endDate) || (enteredDate <= endDate)));

    }, $.validator.format("Please specify a valid date."));

    //**FUNCTIONS

    function adjustSideBanner() {
        var formTableHeight = $('#form-div').height() + $('#public_table').height(),
            policyBarHeight = $('.div-table').height() + $('#policy_section').height(),
            screenHeight = $(window).height() * 0.90,
            newHeight = (formTableHeight > policyBarHeight) ? formTableHeight : policyBarHeight;

        if (newHeight < screenHeight) {
            $('#public_navigation').css('height', screenHeight);
        } else {
            $('#public_navigation').css('height', newHeight);
        };
    }

    //submits the search and date filter form asynchronously
    var ajaxFormSubmit = function ()
    {
        var $selectName = $("select[name = 'IssueYearMinRange']:not(:disabled)");

        if (!$selectName.val() && !$selectName.parent().next().find("select[name = 'IssueYearMaxRange']:not(:disabled)").val())
        {   //checks for inputs for both custom year dropdowns
            alert("Please input a Starting and Ending Year.");
            return false;
        } 

        var $form = $('#form-div form'),
            category = $(".active a").data('subclass'),
            docType = $(".active a").data('subclass-title');

        if (category != undefined && docType != undefined)
        {
            var options = {
                url: $form.attr("action"),//maybe add to this to check for attributes somehow
                cache: true,
                type: $form.attr("method"),
                data: $form.serialize() + "&navBarGroup=" + category + "&navBarItem=" + docType
            };
        } else
        {
            var options = {
                url: $form.attr("action"),//maybe add to this to check for attributes somehow
                type: $form.attr("method"),
                data: $form.serialize()
            };
        }

        $.ajax(options).done(function (data)
        {
            var $target = $($form.attr("data-otf-target")),
                $newHtml = $(data);

            $target.replaceWith($newHtml);

            $newHtml
                .children("table")
                    .effect("highlight");

            postNavbar();
        });

        return false;
    };

    //function to create the misc table and append it to the table
    function createMiscTable(link) {
        var options =
            {
                url: link.attr("action"),
                type: link.attr("method"),
                data: link.serialize()
            };

        $.ajax(options).done(function (data) {
            var id = link.attr("data-otf-target"),
                $target = $(id),
                $newHtml = $(data);

            $newHtml
                .attr("id", id.substring(1, id.length))
                .addClass("misc_table");

            $target
                .replaceWith($newHtml);
        })
    }

    //function to destroy the misc table
    function destroyMiscTable(link) {
        var id = link.attr("data-otf-target"),
            $target = $(id);

        $target.empty();
    }

    //function to destroy preview image
    function losePreview() {
        $('.previewImg')
            .remove();
    }

    function persistEditCheckList() {
        var $table = $('#public_table_inner table tbody');

        $table.find('.main-rows').each(function (i, el) {
            var $this = $(this),
                $tds = $this.find('td'),
                documentId = $tds
                                .eq(1)
                                    .text()
                                        .trim();

            if ($.inArray(documentId, editList) >= 0) {
                $("#" + documentId + "edit").prop('checked', true);
            }
        });
    }

    var modifyEditList = function () {

        if (editList.length < 1) {
            alert('No documents have been selected.');

            return false;
        }

        var options = {
            url: "/Admin/Edit",
            type: "get",
            traditional: true,
            data: {
                EditList: editList,
                folderId: $('#search').val()
            },
            cache: false
        };

        $.ajax(options).done(function (data) {

            $('#main-row').replaceWith($(data));

            $(".edit-issue").each(function () {
                var displayedDate = $(this).val();
                var date = new Date(displayedDate);
                $(this).datepicker({ dateFormat: 'dd M yy' });
            });

            $(".Correspondence").each(function () {
                $(this).prop('disabled', false);
            });
        });

        return false;
    }

    //function to preview image (jpeg)
    function showPreview($this)
    {
        var id = $this.attr('id'),
            left = $this
                        .closest("td")
                        .offset()
                        .left,
            top = $this
                        .closest("td")
                        .offset()
                        .top,
            options =
                {
                    url: $this.attr("href"),
                    data: id,
                    type: "get"
                };

        $.ajax(options).done(function (data)
        {
            var $img = $('<img id="dynamic" class="previewImg" />');

            $img
                .attr('src', options.url)
                .appendTo('body');

            var imgWidth = document
                            .getElementById('dynamic')
                                .clientWidth;

            $img
                .css({ 'position': 'absolute', 'left': left - imgWidth, 'top': top });
        });
    }

    //**EVENTS

    //will adjust left banner bar on load to dynamically match table size
    adjustSideBanner();

    //jackie OUT OF PLACE
    if ($('#issue')[0])
    {
        var $thisParent = $('#issue').parent('A');

        rememeberSort($thisParent);
    }

    //will load the user to have focus inside search bar
    $('#searchInputBox').focus();

    //event for form submit
    $("form[data-otf-ajax='true']").submit(ajaxFormSubmit);

    //image previewer on event
    $("#body").on("mouseover", ".preview_image", function (e)
    {
        $this = $(this).children("a");

        showPreview($this);

        event.stopPropagation();
    });

    $("#body").on("mouseout", ".preview_image", function (e)
    {
        losePreview();
    });

    $("#body").on("click", ".preview", function (e)
    {
        return false;
    });

    //functionality of category/policy slider
    $('#category_policy_toggle').click(function (e)
    {
        $("#category_section, #policy_section").toggleClass("hidden");
    });

    //active navbar class addition ***MAYBE COMBINE WITH ANOTHER
    $(".nav_lists a").on("click", function ()
    {
        $(".nav_lists")
            .find(".active")
                .removeClass("active");

        $(this)
            .parent()
                .addClass("active");
    });

    //event for click on 'MORE', which brings up a misc table
    $("#body").on("click", ".miscTableLink", function ()
    {
        var $link = $(this);

        $link
            .parents('tr')
                .siblings()
                    .find('a').each(function () {
                        destroyMiscTable($(this));
                        $(this)
                            .removeClass('showData')
                        .closest('tr')
                            .removeClass("misc_table");
                    });

        $link
            .toggleClass("showData")
        .closest('tr')
            .toggleClass("misc_table");

        if ($link.hasClass("showData"))
        {
            createMiscTable($link);
        } else
        {
            destroyMiscTable($link);
        }

        return false;
    });

    //event for selecting a min start year from 'Starting Year' dropdown
    $('select[name^="IssueYearMinRange"]').change(function ()
    {
        var value = this.value;

        $('select[name^="IssueYearMinRange"] option').removeAttr('selected');

        $('select[name^="IssueYearMinRange"] option[value="' + value + '"]').attr("selected", "selected");
    });

    //event for selecting a min start year from 'Ending Year' dropdown
    $('select[name^="IssueYearMaxRange"]').change(function ()
    {
        var value = this.value;

        $('select[name^="IssueYearMaxRange"] option').removeAttr('selected');

        $('select[name^="IssueYearMaxRange"] option[value="' + value + '"]').attr("selected", "selected");
    });

    //event for clicking the backspace or enter key in select scenerios
    $(document).bind("keydown", function (e)
    {
        var rx = /INPUT|SELECT|TEXTAREA/i; //will be used to check which markup you are in while clicking on backspace

        if (e.which == 8)
        { // 8 is 'backspace' in ASCII
            if (rx.test(e.target.tagName) && e.target.id == "formSubmitId" || !rx.test(e.target.tagName) || e.target.disabled || e.target.readOnly)
            {
                $('#clearButton').click();
                $('#searchInputBox').focus();
                e.preventDefault();
            }
        } else if (e.which == 13)
        { // 13 is 'enter' in ASCII
            if (e.target.id != "searchInputBox")
            {
                $('#formSubmitId').click();
                e.preventDefault();
            }
        } else if (e.which == '220')
        { // '\' button
            var yearInputMin = $("select[name = 'IssueYearMinRange']:not(:disabled)").val(),
                yearInputMax = new Date().getFullYear();

            if ($("select[name = 'IssueYearMaxRange']:not(:disabled)")[0])
            { //confirms in custom range

                if (yearInputMin == "")
                {
                    yearInputMin = $("select[name = 'IssueYearMinRange']:not(:disabled) option:nth-child(2)").text();
                }
                
                if ($("select[name = 'IssueYearMaxRange']:not(:disabled)").val() != "")
                {
                    yearInputMax = $("select[name = 'IssueYearMaxRange']:not(:disabled)").val();
                }
            }

            if (yearInputMin == -100)
            {
                yearInputMin = $("select[name = 'IssueYearMinRange']:disabled option:nth-child(2)").text();
            } else if (yearInputMin < 0)
            {
                yearInputMin = (new Date().getFullYear() + parseInt(yearInputMin)); //yearInput would be a negative value
            }
        }
    });

    $(document).ajaxComplete(function ()
    {
        adjustSideBanner();
        persistEditCheckList();

        if (!$('.fa-sort-asc')[0] && !$('.fa-sort-desc')[0])
        { //will only run if sort wasnt run right before this request
            var thisParent = $('#issue').parent('A');
            rememeberSort(thisParent);
        }
    });

    $("#body").on("change", "#public_table input[type=checkbox]", function ()
    {
        if ($(this).prop('checked'))
        {
            editList.push($(this).val().trim());
        } else
        {
            var removeItem = $(this).val();
            editList.splice($.inArray(removeItem, editList), 1);
        }
    });


    $(".edit-issue").datepicker({ dateFormat: 'dd M yy' }); //not sure if needed

    $('#clearButton').on("click", function ()
    {
        var $this = $(this);
            options = {
                        url: $this.attr('href') + "&IssueYearMinRange=-1",
                        type: "GET"
                      };

        $.ajax(options).done(function (data)
            {
                var $target = $('#public_table'),
                    $newHtml = $(data);

                $target.replaceWith($newHtml);    
                complete: clearFields($this.prop('id'));
                success: updateCurrentCount();
            });

        return false;
    });

    var downloadAllDocuments = function ()
    {
        $("#downloadForm").submit();

        return false;
    };

    $("body").on("click", "#editOptionsSubmit", function ()
    {
        choice = $('#editOptions option:selected').text();

        if (choice == "Edit These Files")
        {
            modifyEditList();
        } else if (choice == "Download all Public Documents")
        {
            downloadAllDocuments();
        } else
        {
            alert("No option was selected");
        }

        return false;
    });

    $("#body").on("submit", "form#updateListSubmit", function (event)
    {
        $('form#updateListSubmit').validate();

        $('.edit-rows :not(.input_class) input').each(function ()
        {
            $(this).prop("tagName");

            $(this).rules("add", {
                required: true
            });

            $(".edit-issue").each(function ()
            {
                $(this).rules("add", { regex: "^[0-3][0-9]\\s[A-Z][a-z]{2}\\s[1-2][0-9]{3}$" });
            });
        });

        if ($("form#updateListSubmit").validate().form())
        {
            console.log("validates");
        } else
        {
            console.log("does not validate");
            event.preventDefault();
        }
    });

    $("#body").on("click", ".selectAll", function ()
    {
        if (!$(this).is(':checked'))
        {
            $("table.table tbody td:last-child() input:checkbox:checked").click();
        } else
        {
            $("table.table tbody td:last-child() input:checkbox:not(:checked)").click();
        }
    });

    //event for selecting a 'records per page' from dropdown
    $('#fromYear').change(function ()
    {
        var value = this.value;

        $('select[name^="IssueYearMinRange"] option').removeAttr('selected');
        $('select[name^="IssueYearMinRange"] option[value="' + value + '"]').attr("selected", "selected");

        IssueYearMinRange = $('#fromYear option:selected').val();

        ajaxFormSubmit();
    });

    $('#customDates').on("click", function ()
    {
        if ($('#customDates').is(':checked'))
        {
            $('#fromYear').prop("disabled", true);
            $('.customDates').children().prop("disabled", false).children().addBack().css("display", "inline-block");
        } else
        {
            $('#fromYear').prop("disabled", false);
            $('.customDates').children().prop("disabled", true).children().addBack().css("display", "none");
        }
    });

    $('.navLink').on("click", function ()
    {
        $this = $(this);

        var selectYearMin = $("select[name = 'IssueYearMinRange']:not(:disabled)").val(),
            selectYearMax = ($("select[name = 'IssueYearMaxRange']:not(:disabled)").val()) ? $("select[name = 'IssueYearMaxRange']:not(:disabled)").val() : "",
            selectMonthMin = $("select[name = 'IssueMonthMinRange']:not(:disabled)").val(),
            selectMonthMax = $("select[name = 'IssueMonthMaxRange']:not(:disabled)").val(),
            searchTerm = $('#searchInputBox').val();

        if (!selectYearMin)
        {
            alert("Please input a Starting and Ending Year.");
            return false;
        }

        var options =
            {
                url: $this.attr('href'),
                data: "&IssueYearMinRange=" + selectYearMin + "&IssueYearMaxRange=" + selectYearMax + "&IssueMonthMinRange=" + selectMonthMin + "&IssueMonthMaxRange=" + selectMonthMax + "&searchTerm=" + searchTerm,
                type: "GET",
            };

        $.ajax(options).done(function (data)
        {
            var $target = $('#public_table'),
                $newHtml = $(data);

            $target.replaceWith($newHtml);

            complete: postNavbar();
        });

        return false;
    });

    $("#body").on("click", "#allDocs", function ()
    {
        $('#fromYear').val('-100').trigger('change');

        return false;
    });

    $("#body").on("click", "#error_details_checkbox", function ()
    {
        if ($('#error_details_checkbox').is(':checked'))
        {
            $('#error_details').css("visibility", "visible");
        } else
        {
            $('#error_details').css("visibility", "collapse");
        };
    });
});