$(document).ready(function () {
    $("button").click(function () {
        search();
    });

    $('input').keypress(function (e) {
        if (e.keyCode == 13) {
            search();
        }   
    });

    $('input').keyup(function () {
        $("#suggestions").empty();
        var userSearch = $(this).val();
        $.ajax({
            url: "Suggestion.asmx/searchTrie",
            type: "POST",
            dataType: "json",
            data: JSON.stringify({ search: userSearch }),
            contentType: "application/json; charset=utf-8",
            success: function (result) {
                $("#suggestions").empty();
                var resultArray = JSON.parse(result.d);
                displaySuggestions(resultArray, userSearch);
            }
        });
    });
});


function displaySuggestions(data, search) {
    var div = $("<div id=suggestion></div>")
    if (data.length == 0 && search.trim() != "") {
        div.append($("<p id='suggestionsItem'></p>").append("No suggestions found for: " + search));
    } else {
        for (var i = 0; i < data.length; i++) {
            div.append($("<p id='suggestionsItem'></p>").append(data[i].split("_").join(" ")));
        }
    }
    $("#suggestions").append(div);
}


function search() {
    $("#suggestions").empty();
    $("#results").empty();
    $("#right").empty();
    var search = $("input").val().toLowerCase().trim();
    var nbaSearch = search.split(' ').join('+');
    $.ajax({
        crossDomain: true,
        contentType: "application/json; charset=utf-8",
        url: "http://ec2-52-24-135-255.us-west-2.compute.amazonaws.com/pa1/index.php?search=" + nbaSearch,
        data: {},
        dataType: "jsonp",
        success: function (result) {
            if (result.length > 0) {
                displayNBAPlayer(result);
            }
        }
    });

    $.ajax({
        url: "Admin.asmx/checkCache",
        type: "POST",
        dataType: "json",
        data: JSON.stringify({ search: search }),
        contentType: "application/json; charset=utf-8",
        success: function (result) {
            //$("#results").empty();
            var resultArray = JSON.parse(result.d);
            displayResults(resultArray, search);
        }
    });
}

function displayResults(data, search) {
    $("#suggestions").empty();
    //console.log(data);
    if (data.length > 0) {
        for (var i = 0; i < data.length; i += 3) {
            $("#results").append($('<a href="' + data[i + 1] + '" target="_blank" id="resultTitle"></a>').append(data[i]));
            $("#results").append($("<p id='resultUrl'></p>").append(data[i + 1]));
            $("#results").append($("<p id='resultDate'></p>").append("Posted on: " + data[i + 2]));
        }
    } else {
        $("#results").append($("<p id='noResult'></p>").append("No results found for: " + search));
    }
}

function displayNBAPlayer(data) {
    //$("#right").empty();
    //console.log(data[0]);
    var player = data[0];
    var div = $("<div id='nba'></div>");
    var image = "http://i.cdn.turner.com/nba/nba/.element/img/2.0/sect/statscube/players/large/" + player.FullName.toLowerCase().replace(' ', '_') + ".png";
    var team = "http://a.espncdn.com/combiner/i?img=/i/teamlogos/nba/500/" + player.Team + ".png&h=100&w=100";
    div.append($('<img src="' + image + '" id="nbaImage"></img> <br>'));
    div.append($("<p id='nbaName'></p>").append(player.FullName));

    div.append($('<img src="' + team + '" id="nbaTeam"></img>'));
    var table = $("<table></table>");
    var titleRow = $("<tr id=nbaTitleRow></tr>");
    var statRow = $("<tr id=nbaStatRow></tr>");
    titleRow.append($("<th class='nbaTitle'></th>").append("PPG"));
    titleRow.append($("<th class='nbaTitle'></th>").append("GP"));
    titleRow.append($("<th class='nbaTitle'></th>").append("3PTM"));
    titleRow.append($("<th class='nbaTitle'></th>").append("RB"));
    titleRow.append($("<th class='nbaTitle'></th>").append("AST"));
    titleRow.append($("<th class='nbaTitle'></th>").append("STL"));
    titleRow.append($("<th class='nbaTitle'></th>").append("BLK"));
    titleRow.append($("<th class='nbaTitle'></th>").append("TO"));
    statRow.append($("<td class='nbaStat'></td>").append(player.PPG));
    statRow.append($("<td class='nbaStat'></td>").append(player.GP));
    statRow.append($("<td class='nbaStat'></td>").append(player["3PT_M"]));
    statRow.append($("<td class='nbaStat'></td>").append(player.RB_Tot));
    statRow.append($("<td class='nbaStat'></td>").append(player.Ast));
    statRow.append($("<td class='nbaStat'></td>").append(player.Stl));
    statRow.append($("<td class='nbaStat'></td>").append(player.Blk));
    statRow.append($("<td class='nbaStat'></td>").append(player.TO));
    table.append(titleRow);
    table.append(statRow);
    div.append(table);
    $("#right").append(div);


}