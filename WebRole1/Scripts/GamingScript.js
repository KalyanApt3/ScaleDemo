
$(document).ready(function () {
    $("#btnStart").click(function () {
        ProcessData();
    });
});

function ProcessData() {
    debugger;
    var NumberOfDocumentsToInsert = $("#txtNumberOfDocumentsToInsert").val();
    var Throughput = $("#ThroughptSlider").val();

    //call controller ation method
    $.ajax({
        url: "/Home/InsertFiles",
        type: "POST",
        dataType: "json",
        data: '{NumberOfDocumentsToInsert:"' + NumberOfDocumentsToInsert + '",Throughput:"' + Throughput + '"}',
        //data: JSON.stringify({ NumberOfDocumentsToInsert: NumberOfDocumentsToInsert, Throughput: Throughput }),
        contentType: "application/json",        
        success: function (data)
        {
            alert(data);
        },       
        error: function (xhr, status, error) {
            var responseTitle = $(xhr.responseText).filter('title').get(0);
            alert($(responseTitle).text());
        }
    });
}