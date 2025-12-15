function RulesSelected() {

    var ruleFilterSelectAll = $("#ruleFilterSelectAll");
    if (ruleFilterSelectAll.is(":checked")) {
        return "all";
    }

    var rulesSelectElement = $("#ruleFilterTable");
    var rulesArray = new Array();
    var rulesSelector = "";

    $(rulesSelectElement).find(":checked").each(function () {
        var ruleId = $(this).attr("data-debug-rule");
        rulesArray.push("[data-debug-rule='" + ruleId + "']");
    });
    if (rulesArray) {
        rulesSelector = rulesArray.join(",");
    }

    return rulesSelector;
}

function MemoryDumpsSelected() {

    var memoryDumpsArray = new Array();
    var memoryDumpFilterSelectAll = $("#memoryDumpFilterSelectAll");
    if (memoryDumpFilterSelectAll.is(":checked")) {
        return "all";
    }

    var memoryDumpSelectElement = $("#memoryDumpFilterTable");
    var memoryDumpsSelector = "";

    $(memoryDumpSelectElement).find(":checked").each(function () {
        var memoryDumpId = $(this).attr("data-debug-dump");
        memoryDumpsArray.push("[data-debug-dump='" + memoryDumpId + "']");
    });

    if (memoryDumpsArray) {
        memoryDumpsSelector = memoryDumpsArray.join(",");
    }

    return memoryDumpsSelector;
}

function CheckAllMemoryDumps() {
    var memoryDumpSelectElement = $("#memoryDumpFilterTable");
    $(memoryDumpSelectElement).find(":input").each(function () {
        $(this).prop("checked", true);
        $(this).attr("data-is-checked", "true");
    });
}

function CheckAllRules() {

    var rulesSelectElement = $("#ruleFilterTable");
    $(rulesSelectElement).find(":input").each(function () {
        $(this).prop("checked", true);
        $(this).attr("data-is-checked", "true");
    });
}

function SaveFiltersState() {

    var memoryDumpFilterSelectAll = $("#memoryDumpFilterSelectAll");
    if (memoryDumpFilterSelectAll.is(":checked")) {
        memoryDumpFilterSelectAll.attr("data-is-checked", "true");
        CheckAllMemoryDumps();
    }
    else {
        memoryDumpFilterSelectAll.attr("data-is-checked", "false");
    }

    var memoryDumpSelectElement = $("#memoryDumpFilterTable");
    $(memoryDumpSelectElement).find(":input").each(function () {
        if ($(this).is(":checked")) {
            $(this).attr("data-is-checked", "true");
        }
        else {
            $(this).attr("data-is-checked", "false");

        }

    });

    var ruleFilterSelectAll = $("#ruleFilterSelectAll");
    if (ruleFilterSelectAll.is(":checked")) {
        ruleFilterSelectAll.attr("data-is-checked", "true");
        CheckAllRules();

    }
    else {
        ruleFilterSelectAll.attr("data-is-checked", "false");

    }

    var rulesSelectElement = $("#ruleFilterTable");
    $(rulesSelectElement).find(":input").each(function () {
        if ($(this).is(":checked")) {
            $(this).attr("data-is-checked", "true");
        }
        else {
            $(this).attr("data-is-checked", "false");

        }
    });

}

function RevertFilterState() {


    var memoryDumpFilterSelectAll = $("#memoryDumpFilterSelectAll");
    if (memoryDumpFilterSelectAll.attr("data-is-checked") == "true") {
        memoryDumpFilterSelectAll.prop("checked", true);
    }
    else {
        memoryDumpFilterSelectAll.prop("checked", false);
    }

    var memoryDumpSelectElement = $("#memoryDumpFilterTable");
    $(memoryDumpSelectElement).find(":input").each(function () {
        if ($(this).attr("data-is-checked") == "true") {
            $(this).prop("checked", true);
        }
        else {
            $(this).prop("checked", false);

        }

    });

    var ruleFilterSelectAll = $("#ruleFilterSelectAll");
    if (ruleFilterSelectAll.attr("data-is-checked") == "true") {
        ruleFilterSelectAll.prop("checked", true);
    }
    else {
        ruleFilterSelectAll.prop("checked", false);

    }

    var rulesSelectElement = $("#ruleFilterTable");
    $(rulesSelectElement).find(":input").each(function () {
        if ($(this).attr("data-is-checked") == "true") {
            $(this).prop("checked", true)
        }
        else {
            $(this).prop("checked", false)

        }
    });

}

function FilterData() {
    SaveFiltersState();

    var rulesSelector = RulesSelected();
    var memoryDumpsSelector = MemoryDumpsSelected();

    var jQuerySelectorDetails = $("#analysisDetailsGroup, #tableOfContents");
    //Hide all parents in the Analysis Details Section first
    jQuerySelectorDetails.find("div[data-debug-details='parent']").hide();


    var jQuerySelectorSummary = $("#analysisSummaryGroup");
    //Hide everything in the SUmmary Section first
    jQuerySelectorSummary.find("tr[data-is-row='true']").hide();

    $("#boxErrors").text("0");
    $("#boxWarnings").text("0");
    $("#boxInformations").text("0");
    $("#boxNotifications").text("0");

    $("#boxMemoryDumps").text("0/0");
    $("#boxRules").text("0/0");

    //Update MemoryDumps Box
    var totalMemoryDumps = $("#memoryDumpFilterTable").find("[data-is-row='true']").length;
    var selectedMemoryDumps = $("#memoryDumpFilterTable").find(":checked").length;
    $("#boxMemoryDumps").text(selectedMemoryDumps + '/' + totalMemoryDumps);

    //Update Rules Box
    var totalRules = $("#ruleFilterTable").find("[data-is-row='true']").length;
    var selectedRules = $("#ruleFilterTable").find(":checked").length;
    $("#boxRules").text(selectedRules + '/' + totalRules);


    //Hide all dumps inside the parents rules
    $("div[data-debug-details='dump']").hide();
    $("div[data-debug-details='parent']").hide();

    if (rulesSelector.length <= 0) {

        return;
    }

    if (memoryDumpsSelector.length <= 0) {
        return;
    }

    //Analysis Details

    var ruleSelectorForDetails = rulesSelector;
    var memoryDumpSelectorForDetails = memoryDumpsSelector;


    if (rulesSelector.toLowerCase() == "all") {
        ruleSelectorForDetails = "";
    }

    if (memoryDumpsSelector.toLowerCase() == "all") {
        memoryDumpSelectorForDetails = "";
    }

    var jQuerySelectorShowDetails = "div[data-debug-details='parent']" + ruleSelectorForDetails;
    var parentsToDisplay = jQuerySelectorDetails.find(jQuerySelectorShowDetails);
    parentsToDisplay.show();

    var jQueryRulesDetailsSelector = "div[data-debug-details='dump']" + memoryDumpSelectorForDetails;
    parentsToDisplay.find(jQueryRulesDetailsSelector).show();


    //Analysis Summary

    var ruleSelectorForAnalysis = rulesSelector;
    var memoryDumpSelectorForAnalysis = memoryDumpsSelector;

    if (rulesSelector == "all" && memoryDumpsSelector == "all") {
        $("tr[data-is-row='true']").show();
    }

    if (memoryDumpsSelector.toLowerCase() == "all") {
        memoryDumpSelectorForAnalysis = "";
    }


    var errorsSummary = "";
    var warningsSummary = "";
    var informationSummary = "";
    var notificationSummary = "";


    var jQuerySelectorShowSummary = "tr[data-is-row='true']" + memoryDumpSelectorForAnalysis;

    if (rulesSelector.toLowerCase() == "all") {
        errorsSummary = $("#errorsAnalysisSummarySection").find(jQuerySelectorShowSummary);
        errorsSummary.show();
        $("#boxErrors").text(errorsSummary.length);
        warningsSummary = $("#warningsAnalysisSummarySection").find(jQuerySelectorShowSummary);
        warningsSummary.show();
        $("#boxWarnings").text(warningsSummary.length);
        informationSummary = $("#informationsAnalysisSummarySection").find(jQuerySelectorShowSummary);
        informationSummary.show();
        $("#boxInformations").text(informationSummary.length);
        notificationSummary = $("#notificationsAnalysisSummarySection").find(jQuerySelectorShowSummary);
        notificationSummary.show();
        $("#boxNotifications").text(notificationSummary.length);
        //jQuerySelectorSummary.find(jQuerySelectorShowSummary).show();
        return;
    }

    errorsSummary = $("#errorsAnalysisSummarySection").find(jQuerySelectorShowSummary).filter(ruleSelectorForAnalysis);
    errorsSummary.show();
    $("#boxErrors").text(errorsSummary.length);
    warningsSummary = $("#warningsAnalysisSummarySection").find(jQuerySelectorShowSummary).filter(ruleSelectorForAnalysis);
    warningsSummary.show();
    $("#boxWarnings").text(warningsSummary.length);
    informationSummary = $("#informationsAnalysisSummarySection").find(jQuerySelectorShowSummary).filter(ruleSelectorForAnalysis);
    informationSummary.show();
    $("#boxInformations").text(informationSummary.length);
    notificationSummary = $("#notificationsAnalysisSummarySection").find(jQuerySelectorShowSummary).filter(ruleSelectorForAnalysis);
    notificationSummary.show();
    $("#boxNotifications").text(notificationSummary.length);
    //jQuerySelectorSummary.find(jQuerySelectorShowSummary).filter(ruleSelectorForAnalysis).show();
}

function UpdateTableColors() {
    var row = 0;

    $("table[data-debug-type='table']").each(function () {
        var table = this;
        row = 1;
        $(table).find("tr[data-is-row='true']:not(:hidden)").each(function () {
            if (row == 0) {
                $(this).css("background-color", "#eeeeee");

                if ($(this).context.firstChild != null) {
                    $(this).children("td").each(function () {
                        $(this).css("border", "1px solid white");
                    });
                    row = 1;
                }
            }
            else {
                $(this).css("background-color", "white");

                if ($(this).context.firstChild != null) {
                    $(this).children("td").each(function () {
                        $(this).css("border", "1px solid #eeeeee");
                    });
                    row = 0;
                }
            }
        });
    });
}

function doResize() {
    var toc = $("#tableOfContents");
    var cssRules = document.styleSheets[1].cssRules;

    var contentWidth = $("#reportTitleBar").width();
    var windowWidth = window.innerWidth;
    var wideTocWillFit = (windowWidth - contentWidth) / 2 > 230;

    for (var i = 0; i < cssRules.length; i++) {
        var tocRule = cssRules[i];

        if (tocRule.selectorText == ".fixedTOCTopDiv") {
            if (wideTocWillFit)
                tocRule.style.width = "215px";
            else
                tocRule.style.width = "42px";
        }
    }
    //var toc2 = toc[0];
    //toc.css("width", "auto");
    //var autoWidth = toc[0].scrollWidth;

    //if (window.innerWidth > 1675)
    //    toc.css("width", "215px");
    //else 
    //    toc.css("width", "42px");
}

$(window).resize(function () {
    doResize();
});

$(document).ready(function () {
    doResize();

    var totalRules = $("#ruleFilterTable").find("input[type=checkbox]").length;
    var totalMemoryDumps = $("#memoryDumpFilterTable").find("input[type=checkbox]").length;
    var rulesFilterSelectAll = $("#ruleFilterSelectAll");
    var memoryDumpsFilterSelectAll = $("#memoryDumpFilterSelectAll");

    //hides warning about scripting
    $("#ScriptOff-s").toggle();

    SaveFiltersState();
    FilterData();
    UpdateTableColors();

    $("#button").click(function () {
        $("tr[data-is-row='true']:not(hidden)").slideUp();
    });

    //Display the dialog to select the filters for the memory dumps
    $("#btnFilterMemoryDump").click(function () {
        $("#memoryDumpFilterSection").css({ display: 'block' });
    });

    $("#closememoryDumpFilterSelection").click(function () {
        $("#memoryDumpFilterSection").css({ display: 'none' });
        RevertFilterState();
    });

    $("#applymemoryDumpFilterSelection").click(function () {
        if ($(this).attr("class").toLowerCase().indexOf("unselected") !== -1)
            return;
        $("#memoryDumpFilterSection").css({ display: 'none' });

        FilterData();
        UpdateTableColors();

    });



    //Display the dialog to select the filters for the rules
    $("#btnFilterRule").click(function () {
        $("#ruleFilterSection").css({ display: 'block' });
    });

    $("#closeruleFilterSelection").click(function () {
        $("#ruleFilterSection").css({ display: 'none' });
    });

    $("#applyruleFilterSelection").click(function () {
        if ($(this).attr("class").toLowerCase().indexOf("unselected") !== -1)
            return;

        $("#ruleFilterSection").css({ display: 'none' });

        FilterData();
        UpdateTableColors();
    });

    //Handler when the Expand/Collapse button is clicked
    $("#btnAnalysisSummary").click(function () {
        $("#analysisSummaryGroup").toggle("slow", function () {
            $("#btnAnalysisSummary").toggleClass("expandCollapseButton-collapsed");
        });
    });

    //Handler when the Expand/Collapse button is clicked
    $("#btnAnalysisDetails").click(function () {
        $("#analysisDetailsGroup").toggle("slow", function () {
            $("#btnAnalysisDetails").toggleClass("expandCollapseButton-collapsed");
        });
    });

    //Handler when the Expand/Collapse button is clicked
    $("#btnRuleSummary").click(function () {
        $("#ruleSummaryGroup").toggle("slow", function () {
            $("#btnRuleSummary").toggleClass("expandCollapseButton-collapsed");
        });
    });

    //Handler when the Errors Box is clicked, it will take scroll to that section
    $("#btnBoxErrors").click(function () {
        scrollTop("#errorsAnalysisSummarySection");
    });

    //Handler when the Warnings Box is clicked, it will take scroll to that section
    $("#btnBoxWarnings").click(function () {
        scrollTop("#warningsAnalysisSummarySection");
    });

    //Handler when the Information Box is clicked, it will take scroll to that section
    $("#btnBoxInfortmations").click(function () {
        scrollTop("#informationsAnalysisSummarySection");
    });

    //Handler when the Notifications Box is clicked, it will take scroll to that section
    $("#btnBoxNotifications").click(function () {
        scrollTop("#notificationsAnalysisSummarySection");
    });

    function scrollTop(id) {
        var offset = $(id).offset();
        $(window).scrollTop(offset.top - 10);
    }

    $("#ruleFilterSelectAll").change(function () {
        var rulesSelectElement = $("#ruleFilterTable");
        var checked = $(this).is(":checked");
        $(rulesSelectElement).find("input[type=checkbox]").each(function () {
            this.checked = checked;
        });
    });


    $("#memoryDumpFilterSelectAll").change(function () {
        var memoryDumpsSelectElement = $("#memoryDumpFilterTable");
        var checked = $(this).is(":checked");
        $(memoryDumpsSelectElement).find("input[type=checkbox]").each(function () {
            this.checked = checked;
        });
    });


    $("#ruleFilterTable").find("input[type=checkbox]").each(function () {
        $(this).change(function () {
            var countChecked = $("#ruleFilterTable").find("input[type=checkbox]").filter(":checked").length;

            if (countChecked == totalRules) {
                rulesFilterSelectAll.prop("checked", true);
            }
            else {
                rulesFilterSelectAll.prop("checked", false);
            }
        });
    });

    $("#memoryDumpFilterTable").find("input[type=checkbox]").each(function () {
        $(this).change(function () {
            var countChecked = $("#memoryDumpFilterTable").find("input[type=checkbox]").filter(":checked").length;

            if (countChecked == totalMemoryDumps) {
                memoryDumpsFilterSelectAll.prop("checked", true);
            }
            else {
                memoryDumpsFilterSelectAll.prop("checked", false);
            }
        });
    });

});

var cX = 0; var cY = 0; var rX = 0; var rY = 0;

function UpdateCursorPosition(e) {
    cX = e.pageX;
    cY = e.pageY;
}

function UpdateCursorPositionDocAll(e) {
    cX = event.clientX;
    cY = event.clientY;
}

if (document.all) {
    document.onmousemove = UpdateCursorPositionDocAll;
}
else {
    document.onmousemove = UpdateCursorPosition;
}

function AssignPosition(d) {
    if (self.pageYOffset) {
        rX = self.pageXOffset;
        rY = self.pageYOffset;
    }
    else if (document.documentElement && document.documentElement.scrollTop) {
        rX = document.documentElement.scrollLeft;
        rY = document.documentElement.scrollTop;
    }
    else if (document.body) {
        rX = document.body.scrollLeft;
        rY = document.body.scrollTop;
    }

    if (document.all) {
        cX += rX;
        cY += rY;
    }

    d.style.left = (cX + 10) + "px";
    d.style.top = (cY + 10) + "px";
}

function HideContent(d) {
    if (d.length < 1) {
        return;
    }
    document.getElementById(d).style.display = "none";
}

function ShowContent(d) {
    if (d.length < 1) { return; }
    var dd = document.getElementById(d);
    AssignPosition(dd); dd.style.display = "block";
}

function ReverseContentDisplay(d) {
    if (d.length < 1) { return; }
    var dd = document.getElementById(d);
    AssignPosition(dd);
    if (dd.style.display == "none") {
        dd.style.display = "block";
    }
    else { dd.style.display = "none"; }
}