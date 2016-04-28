using System;
using System.Collections.Generic;

/// <summary>
/// Trie class for the Trie data structure
/// </summary>
namespace WebRole1
{
    public class Trie
    {
        public Node root { get; private set; }

        public Trie()
        {
            root = new Node();
        }

        /// <summary>
        /// Adds a provided title to the Trie
        /// </summary>
        /// <param name="title"></param>
        public void AddTitle(string title)
        {
            Node temp = root;
            // Loops through title to add characters into appropriate dictionaries
            for (int i = 0; i < title.Length; i++)
            {
                if (!temp.dictionary.ContainsKey(title[i]))
                {
                    temp.dictionary.Add(title[i], new Node());
                }
                temp = temp.dictionary[title[i]];
            }
            // Sets the last character of the title to a leaf node
            temp.eow = true;
        }

        /// <summary>
        /// Navigates through the Trie using the provided search string
        /// to find the index to begin searching for suggestions
        /// </summary>
        /// <param name="search"></param>
        /// <returns>a list of up to 10 search results</returns>
        public List<string> SearchForPrefix(string search)
        {
            Node temp = root;
            string result = "";
            List<string> searchResults = new List<string>();
            for (int i = 0; i < search.Length; i++)
            {
                /* Checks if search exists in all of the search character's dictionaries
                Else return no results. */
                if (temp.dictionary.ContainsKey(search[i]))
                {
                    temp = temp.dictionary[search[i]];
                    result += search[i];
                }
                else
                {
                    break;
                }
            }

            // If all characters of search string exist in the Trie, search for results.
            if (search == result && search != "")
            {
                searchResults = this.SearchForWords(temp, search, searchResults);
            }
            return searchResults;
        }

        /// <summary>
        /// Recursive method to search for up to 10 suggestion results using provided
        /// Node, result string, and list of string results.
        /// </summary>
        /// <param name="temp"></param>
        /// <param name="result"></param>
        /// <param name="searchResults"></param>
        /// <returns>a list of up to 10 search results</returns>
        public List<string> SearchForWords(Node temp, string result, List<string> searchResults)
        {
            // If less than 10 results exist in searchResults, continue searching
            if (searchResults.Count < 10)
            {
                // If Node is a leaf node, add to searchResults
                if (temp.eow == true)
                {
                    searchResults.Add(result);
                }

                /* Adds character to result and calls recursive method to add 
                more characters until result is a leaf node */
                foreach (var dictionary in temp.dictionary)
                {
                    result += dictionary.Key;
                    Node newTemp = dictionary.Value;
                    this.SearchForWords(newTemp, result, searchResults);
                    result = result.Substring(0, result.Length - 1);
                }
            }
            return searchResults;
        }

    }

}
