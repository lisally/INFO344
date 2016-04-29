$(document).ready(function() {
    /* requests search suggestions from searchTrie web method upon
    every key stroke */
    $('input').keyup(function () {
        $("#results").empty();
        var userSearch = $(this).val();
        $.ajax({
            url: "getQuerySuggestions.asmx/searchTrie",
            type: "POST",
            dataType: "json",
            data: JSON.stringify({ search: userSearch }),
            contentType: "application/json; charset=utf-8",
            success: function (result) {
                $("#results").empty();
                var resultArray = JSON.parse(result.d);
                displayResults(resultArray, userSearch);
            }
        });
    });

});

/* displays up to 10 search suggestion results and a no suggestion message
 if no suggestions exist */
function displayResults(result, search)
{
    if (result.length == 0 && search.trim() != "") {
        $("#results").append($("<p></p>").append("No suggestions found for: " + search));
    }
    else
    {
        for (var i = 0; i < result.length; i++) 
        {
            $("#results").append($("<p></p>").append(result[i].split("_").join(" ")));
        }
    }
}

