/// <reference path="_references.js" />

var globalTimestamp = 0;

function formatLongNumber(value) {
    if (value == 0) {
        return 0;
    }
    else {

        if (value <= 999) {
            return value;
        }
            // thousands
        else if (value >= 1000 && value <= 999999) {
            return (value / 1000).toFixed(1) + 'K';
        }
            // millions
        else if (value >= 1000000 && value <= 999999999) {
            return (value / 1000000).toFixed(1) + 'M';
        }
            // billions
        else if (value >= 1000000000 && value <= 999999999999) {
            return (value / 1000000000).toFixed(1) + 'B';
        }
        else {
            return (value / 1000000000000).toFixed(1) + 'T';
        }
    }
}

var chartOptions = {
    TotalDocumentsCreated: {
        id: "#totalDocumentsCreatedChart",
        title: "Total Documents Created",
        docdbProperty: "totalDocumentsCreated",
        valueAxisTitleText: "doc",
        valueAxisStep: 100,
        categoryAxisVisible: true,
        type: "area",
        series: [{
            name: "Total Documents Created",
            data: [],
            color: "#7cca62"
        }]
    },
    DocumentsCreatedPerSecond: {
        id: "#documentsCreatedPerSecondChart",
        title: "Documents Created per second",
        docdbProperty: "documentsCreatedPerSecond",
        valueAxisTitleText: "doc",
        valueAxisStep: 10,
        categoryAxisVisible: true,
        type: "area",
        series: [{
            data: [],
            color: "#7cca62"
        }]
    },
    DocumentsCreatedInLastSecond: {
        id: "#documentsCreatedInLastSecondChart",
        title: "Documents Created in last second",
        docdbProperty: "documentsCreatedInLastSecond",
        valueAxisTitleText: "doc",
        valueAxisStep: 10,
        categoryAxisVisible: true,
        type: "area",
        series: [{
            data: [],
            color: "#7cca62"
        }]
    },
    RequestUnitsPerMonth: {
        id: "#requestUnitsPerMonthChart",
        title: "Request Units per month",
        docdbProperty: "requestUnitsPerMonth",
        valueAxisTitleText: "RU",
        valueAxisStep: 10000,
        categoryAxisVisible: true,
        type: "area",
        series: [{
            data: []
        }]
    },
    RequestUnitsPerSecond: {
        id: "#requestUnitsPerSecondChart",
        title: "Request Units per second",
        docdbProperty: "requestUnitsInLastSecond",
        valueAxisTitleText: "RU",
        valueAxisStep: 100,
        categoryAxisVisible: true,
        type: "area",
        series: [{
            name: "Request Units per second",
            data: []
        }]
    },
    RequestUnitsInLastSecond: {
        id: "#requestUnitsInLastSecondChart",
        title: "Requests Units in last second",
        docdbProperty: "requestUnitsInLastSecond",
        valueAxisTitleText: "RU",
        valueAxisStep: 100,
        categoryAxisVisible: true,
        type: "area",
        series: [
        {
            data: []
        }]
    }
};

var downloadData = function () {
    $.ajax({
        type: "POST",
        url: "/Home/GetMetrix",
        dataType: "json"
    }).done(function (data) {
        if (data != null) {

            var chartTitles = ["TotalDocumentsCreated", "DocumentsCreatedPerSecond", "DocumentsCreatedInLastSecond", "RequestUnitsPerMonth", "RequestUnitsPerSecond", "RequestUnitsInLastSecond"]

            var totalStats = {
                totalDocumentsCreated: 1,
                documentsCreatedPerSecond: 1,
                documentsCreatedInLastSecond: 1,
                requestUnitsPerSecond: 1,
                requestUnitsPerMonth: 1,
                requestUnitsInLastSecond: 1
               
            }

            var tempDoc;
            var docdbProperties = ["totalDocumentsCreated", "documentsCreatedPerSecond", "documentsCreatedInLastSecond", "requestUnitsPerMonth", "requestUnitsPerSecond", "requestUnitsInLastSecond"]

            tempDoc = JSON.parse(data[0]);
            docdbProperties.forEach(function (property) {
                totalStats[property] += tempDoc[property];
            });

            var d = new Date();
            var n = d.getTime();

            chartTitles.forEach(function (element) {
                if (chartOptions[element].series[0].data.length > 10) {
                    var shifted = chartOptions[element].series[0].data.shift();
                }

                chartOptions[element].series[0].data.push(totalStats[chartOptions[element].docdbProperty]);
            });

        } else {
            console.log("ERROR: " + msg);
        }
    }).fail(function (msg) {
        console.log("ERROR: " + JSON.stringify(JSON.parse(error), null, 2));
    })
}


function UpdateThroughput() {
    var Throughput = $("#ThroughptSlider").val();
    $.ajax({
        url: "/Home/UpdateCollectionThrouhput",
        type: "POST",
        traditional: true,
        dataType: "json",
        data: '{Throughput:"' + Throughput + '"}',
        contentType: "application/json",
        success: function (data) {
        },
        error: function (xhr, status, error) {
            var responseTitle = $(xhr.responseText).filter('title').get(0);
        }
    });
}
function GetCurrentThroughput() {
    $.ajax({
        url: "/Home/GetCurrentThroughput",
        type: "POST",
        traditional: true,
        dataType: "json",
        data: '{}',
        contentType: "application/json",
        success: function (data) {
            $('#ThroughptSlider').slider('setValue', data);
            StartGraphProcess();  //update throughput and show graph
        },
        error: function (xhr, status, error) {
            var responseTitle = $(xhr.responseText).filter('title').get(0);
        }
    });
}

//First time graph loading with default throughput (from db)
$(document).ready(function () {
    GetCurrentThroughput();//get throughut here from db and start graph 
});

//throughput onchange - updated graph with given throughput
$(document).ready(function () {
    $("#btnThroughputSlider").click(function () {
        UpdateThroughput(); //updates throughput on change in the slider value

    });
});

var chartTitles = ["TotalDocumentsCreated", "DocumentsCreatedPerSecond", "DocumentsCreatedInLastSecond", "RequestUnitsPerMonth", "RequestUnitsPerSecond", "RequestUnitsInLastSecond"]
var totalStats = { totalDocumentsCreated: 1, documentsCreatedPerSecond: 1, documentsCreatedInLastSecond: 1, requestUnitsPerMonth: 1, requestUnitsPerSecond: 1, requestUnitsInLastSecond: 1 }
var docdbProperties = ["totalDocumentsCreated", "documentsCreatedPerSecond", "documentsCreatedInLastSecond", "requestUnitsPerMonth", "requestUnitsPerSecond", "requestUnitsInLastSecond"]

var JschartTitles = ["TotalDocumentsCreated", "RequestUnitsPerSecond"]
var JstotalStats = { totalDocumentsCreated: 1, requestUnitsPerSecond: 1 }
var JsdocdbProperties = ["totalDocumentsCreated", "requestUnitsPerSecond"]

var RequestUnitsPerSecondData = []; var RequestUnitsPerSecondLblData = []; var TotalDocsCreatedData = []; var TotalDocsCreateLbldData = [];

var JschartOptions = {
    TotalDocumentsCreated: {
        id: "totalDocumentsCreatedChart",
        title: "Total Documents Created",
        docdbProperty: "totalDocumentsCreated"
    },
    RequestUnitsPerSecond: {
        id: "requestUnitsPerSecondChart",
        title: "Request Units per second",
        docdbProperty: "requestUnitsInLastSecond"
    },
};

function StartGraphProcess() {
    JschartTitles.forEach(function (element) {
        createJsChart(JschartOptions[element].id);
    });

    var interval = setInterval(ShowMetrixGraph, 2000);
}

function createJsChart(graphid) {
    var options = {
        options: {
            scales: {
                yAxes: [{
                    ticks: {
                        beginAtZero: true
                    }
                }],
                scaleShowLabels: false
            }
        }
    };

    var data = {
        labels: (graphid == 'requestUnitsPerSecondChart' ? RequestUnitsPerSecondLblData : TotalDocsCreateLbldData),
        datasets: [
            {
                label: (graphid == 'requestUnitsPerSecondChart' ? "Request Units per second" : "Total Documents Created"),
                fillColor: "rgba(220,220,220,0.2)",
                lineTension: 0.1,
                backgroundColor: "rgba(75,192,192,0.4)",
                borderColor: "rgba(75,192,192,1)",
                borderCapStyle: 'butt',
                borderDash: [],
                borderDashOffset: 0.0,
                borderJoinStyle: 'miter',
                pointBorderColor: "rgba(75,192,192,1)",
                pointBackgroundColor: "#fff",
                pointBorderWidth: 1,
                pointHoverRadius: 5,
                pointHoverBackgroundColor: "rgba(75,192,192,1)",
                pointHoverBorderColor: "rgba(220,220,220,1)",
                pointHoverBorderWidth: 2,
                pointRadius: 1,
                pointHitRadius: 10,
                data: (graphid == 'requestUnitsPerSecondChart' ? RequestUnitsPerSecondData : TotalDocsCreatedData),
                spanGaps: false
            }
        ]
    };

    var chartid = document.getElementById(graphid).getContext('2d');

    var myLineChart = new Chart(chartid, {
        type: 'line',
        data: data,
        options: options
    });
}

$(document).ready(function () {
    JschartTitles.forEach(function (element) {
        createJsChart(JschartOptions[element].id);
    });
});

function ShowMetrixGraph() {
    var tempDoc;
    $.ajax({
        url: "/Home/GetMetrix",
        type: "POST",
        traditional: true,
        dataType: "json",
        contentType: "application/json",
        success: function (data) {

            var requestUnitsInLastSecond = (data.items[0].requestUnitsInLastSecond == "" ? 0 : data.items[0].requestUnitsInLastSecond);
            var documentsCreatedInLastSecond = (data.items[0].documentsCreatedInLastSecond == "" ? 0 : data.items[0].documentsCreatedInLastSecond);
            var requestUnitsPerMonth = (data.items[0].requestUnitsPerMonth == "" ? 0 : data.items[0].requestUnitsPerMonth);
            $("#writsInLastSec").text(documentsCreatedInLastSecond);
            $("#RUsInLastSec").text(requestUnitsInLastSecond);
            $("#RUspermonth").text(formatLongNumber(requestUnitsPerMonth));

            var id = data.items[0].id;

            totalStats["totalDocumentsCreated"] = data.items[0].totalDocumentsCreated;
            totalStats["documentsCreatedPerSecond"] = data.items[0].documentsCreatedPerSecond;
            totalStats["documentsCreatedInLastSecond"] = data.items[0].documentsCreatedInLastSecond;
            totalStats["requestUnitsPerMonth"] = data.items[0].requestUnitsPerMonth;
            totalStats["requestUnitsPerSecond"] = data.items[0].requestUnitsPerSecond;
            totalStats["requestUnitsInLastSecond"] = data.items[0].requestUnitsInLastSecond;

            RequestUnitsPerSecondData.push(totalStats["requestUnitsInLastSecond"]); RequestUnitsPerSecondLblData.push("");
            TotalDocsCreatedData.push(totalStats["totalDocumentsCreated"]); TotalDocsCreateLbldData.push("");

            JschartTitles.forEach(function (element) {
                createJsChart(JschartOptions[element].id);
            });
        },
        error: function (xhr, status, error) {
            var responseTitle = $(xhr.responseText).filter('title').get(0);

        }
    });
}