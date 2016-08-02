$(document).ready(function () {
    $("#btnRunQuery").click(function () {
        RunQuery("Run");
    });

    //Next Button is set to display none initially when page loads
    $('#aNext').css("display", "none");
    $("#aNext").click(function () {
        RunQuery("Next");
    });
    settxtareaval();
    $("#drpdownselect").change(function () {
        settxtareaval();
    });

    //Refresh button clears the result area when clicked and it also disable the next button
    $("#refshBtn").click(function () {
        $("#resultJson").text("");
        $('#aNext').css("display", "none");
    });    
});

//Select query is hardcoded in the following drop down function
function settxtareaval() {
    var selectedval = $("#drpdownselect").val();
    var txtareaval = (selectedval == "Select" ? "select * from c" : selectedval);
    $(".txtarea1").val(txtareaval);
}

//When Run button is clicked it excutes the real time query with selected drop down result
function RunQuery(mode) {
    var Query = $(".txtarea1").val();
    var ContinuationToken = $("#spanContinuationToken").val();

    if (mode == "Run")
        ContinuationToken = "";

    $.ajax({
        url: "/Home/metricsQuery",
        type: "POST",
        traditional: true,
        dataType: "json",
        data: '{Query:"' + Query + '",ContinuationToken:"' + ContinuationToken + '"}',
        contentType: "application/json",
        success: function (data) {
            $("#resultJson").text(data.items);
            $("#spanContinuationToken").val(data.ContinuationToken);
            $('#aNext').css("display", "block");
        },
        error: function (xhr, status, error) {
            var responseTitle = $(xhr.responseText).filter('title').get(0);
            alert(responseTitle);
        }
    })
}
// This functionality set the throughput value from and to "Portal"

var minSliderValue = $("#ThroughptSlider").data("slider-min");
var maxSliderValue = $("#ThroughptSlider").data("slider-max");

$('#ThroughptSlider').slider({
    value: 0,
    formatter: function (value) {
        return 'Current value: ' + value;
    }
});

$("#ThroughptSlider").on("keyup", function () {
    var val = Math.abs(parseInt(this.value, 10) || minSliderValue);
    this.value = val > maxSliderValue ? maxSliderValue : val;
    $('#ThroughptSlider').slider('setValue', val);
});


