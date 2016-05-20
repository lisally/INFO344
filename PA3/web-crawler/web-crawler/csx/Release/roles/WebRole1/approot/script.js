$(document).ready(function () {
    var stopped = false;

    getWorkerStatus();
    getCpuRam();
    getStats();
    setIntervals();

    $("#start").click(function () {
        stopped = false;
        $.ajax({
            url: "admin.asmx/start",
            type: "POST",
            dataType: "json",
            contentType: "application/json; charset=utf-8",
            success: function(result) {
                var display = result.d;
                $("#status").empty();
                $("#status").append($("<p></p>").append(display)).fadeIn(500);
                setTimeout(function () { $("#status").fadeOut(500); }, 3000)

            }
        });
    });

    $("#stop").click(function () {
        stopped = true;
        $.ajax({
            url: "admin.asmx/stop",
            type: "POST",
            dataType: "json",
            contentType: "application/json; charset=utf-8",
            success: function (result) {
                var display = result.d;
                $("#status").empty();
                $("#status").append($("<p></p>").append(display)).fadeIn(500)
                setTimeout(function () { $("#status").fadeOut(500) }, 3000);
            }
        });
    });

    $("#clear").click(function () {
        stopped = false;
        $("#status").empty();
        $("#status").append($("<p></p>").append("Clearing Tables and Queues. Please Wait 50 Seconds...")).fadeIn(500)

        $.ajax({
            url: "admin.asmx/clear",
            type: "POST",
            dataType: "json",
            contentType: "application/json; charset=utf-8",
            success: function (result) {
                var display = result.d;
                $("#status").empty();
                $("#status").append($("<p></p>").append(display)).fadeIn(500)
                setTimeout(function () { $("#status").fadeOut(500) }, 3000)
            }
        });
    });

    $("#search").click(function () {
        $("#search-result").empty();
        var search = $("input").val();

        $.ajax({
            url: "admin.asmx/searchUrl",
            type: "POST",
            dataType: "json",
            data: JSON.stringify({ url: search }),
            contentType: "application/json; charset=utf-8",
            success: function (result) {
                var data = JSON.parse(result.d);
                $("#search-result").append($("<p></p>").append(data));
            }
        });
    });

    function setIntervals() {
        setInterval(getWorkerStatus, 3000);
        setInterval(getCpuRam, 3000);
        setInterval(getStats, 3000);
        setInterval(getLists, 1000);
    }

    function getWorkerStatus()
    {
        $.ajax({
            url: "admin.asmx/getWorkerStatus",
                type: "POST",
                dataType: "json",
                contentType: "application/json; charset=utf-8",
                success: function (result) {
                    var data = JSON.parse(result.d);

                    if (stopped == true) {
                        data = "Stopping";
                    }

                    $("#worker-status").empty();
                    
                    $("#worker-status").append($("<p class='title'></p>").append("Worker Status").append($("<p class='data'></p>").append(data)));
                }
        });
    }

    function getCpuRam() {
        $.ajax({
            url: "admin.asmx/getCpuRam",
            type: "POST",
            dataType: "json",
            contentType: "application/json; charset=utf-8",
            success: function (result) {
                var data = JSON.parse(result.d);

                $("#cpu").empty();
                $("#ram").empty();

                $("#cpu").append($("<p class='title'></p>").append("CPU Usage").append($("<p class='data'></p>").append(data[0] + "%")));
                $("#ram").append($("<p class='title'></p>").append("RAM Available").append($("<p class='data'></p>").append(data[1] + " MB")));
            }
        });
    }

    function getStats() {
        $.ajax({
            url: "admin.asmx/getStats",
            type: "POST",
            dataType: "json",
            contentType: "application/json; charset=utf-8",
            success: function (result) {
                var data = JSON.parse(result.d);

                $("#index-size").empty();
                $("#urls-crawled").empty();
                $("#queue-size").empty();

                $("#index-size").append($("<p class='title'></p>").append("Url Table Size").append($("<p class='data'></p>").append(data[0])));
                $("#urls-crawled").append($("<p class='title'></p>").append("Urls Crawled").append($("<p class='data'></p>").append(data[1])));
                $("#queue-size").append($("<p class='title'></p>").append("Url Queue Size").append($("<p class='data'></p>").append(data[2])));

            }
        });
    }

    function getLists() {
        $.ajax({
            url: "admin.asmx/getLists",
            type: "POST",
            dataType: "json",
            contentType: "application/json; charset=utf-8",
            success: function (result) {
                var data = JSON.parse(result.d);

                $("#url-list").empty();
                $("#error-list").empty();

                $("#url-list").append($("<p class='title'></p>").append("Last 10 Urls Crawled"));
                $("#error-list").append($("<p class='title'></p>").append("Last 10 Errors Found"));

                var urlList = data[0].split(",");

                for (var i = 0; i < urlList.length; i++)
                {
                    $("#url-list").append($("<p class='list-item'></p>").append(urlList[i]));
                }

                var errorList = data[1].split(",");
                for (var j = 0; j < errorList.length - 1; j+=2)
                {
                    $("#error-list").append($("<p class='list-item'></p>").append(errorList[j+1] + "<br>" + errorList[j]));
                }
            }
        });
    }
});
