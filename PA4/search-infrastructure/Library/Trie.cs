using System.Collections.Generic;

namespace Library
{
    public class Trie
    {
        private Node root { get; set; }

        public Trie()
        {
            root = new Node();
        }

        public void AddTitle(string title)
        {
            Node temp = root;

            if (temp.dictionary == null && temp.list == null)
            {
                temp.dictionary = new Dictionary<char, Node>();
                temp.list = new List<string>();
            }

            if (temp.list != null)
            {
                if (temp.list.Count < 8)
                {
                    temp.list.Add(title);
                }
                else
                {
                    foreach (string listTitle in temp.list)
                    {
                        if (!temp.dictionary.ContainsKey(listTitle[0]))
                        {
                            temp.dictionary.Add(listTitle[0], new Node());
                            temp.dictionary[listTitle[0]].list = new List<string>();
                        }
                        temp.dictionary[listTitle[0]].list.Add(listTitle.Substring(1));
                    }
                    temp.list = null;
                }
            }

            if (temp.list == null)
            {
                for (int i = 0; i < title.Length; i++)
                {
                    if (temp.list != null)
                    {
                        if (temp.list.Count < 8)
                        {
                            if (title.Length < 1)
                            {
                                temp.eow = true;
                            }
                            temp.list.Add(title.Substring(i));

                            break;
                        }
                        else
                        {
                            temp.eow = false;
                            foreach (string listTitle in temp.list)
                            {
                                if (temp.dictionary == null)
                                {
                                    temp.dictionary = new Dictionary<char, Node>();
                                }

                                if (listTitle.Length > 0)
                                {
                                    if (!temp.dictionary.ContainsKey(listTitle[0]))
                                    {
                                        temp.dictionary.Add(listTitle[0], new Node());
                                        temp.dictionary[listTitle[0]].list = new List<string>();
                                    }
                                    temp.dictionary[listTitle[0]].list.Add(listTitle.Substring(1));
                                }
                                else
                                {
                                    temp.eow = true;
                                }
                            }
                            temp.list = null;
                        }

                    }

                    if (temp.dictionary.Count > 0)
                    {
                        if (!temp.dictionary.ContainsKey(title[i]))
                        {
                            temp.dictionary.Add(title[i], new Node());
                            temp.dictionary[title[i]].list = new List<string>();
                        }
                        temp = temp.dictionary[title[i]];
                        if (title.Substring(i).Length == 1)
                        {
                            temp.eow = true;
                        }

                    }
                }

                if (temp.dictionary == null && temp.list.Count == 0)
                {
                    temp.eow = true;
                }
            }

        }

        public List<string> SearchForPrefix(string search)
        {
            Node temp = root;
            string result = "";
            bool dictionary = false;
            bool list = false;
            List<string> searchResults = new List<string>();
            for (int i = 0; i < search.Length; i++)
            {
                if (temp.dictionary != null)
                {
                    if (temp.dictionary.ContainsKey(search[i]))
                    {
                        temp = temp.dictionary[search[i]];
                        result += search[i];
                    }

                    if (search.Equals(result))
                    {
                        dictionary = true;
                        search = "";
                    }
                }
                else if (temp.list != null)
                {
                    if (temp.list.Count > 1)
                    {
                        foreach (string title in temp.list)
                        {
                            if (search.Length > result.Length)
                            {
                                search = search.Substring(result.Length);
                                if (title.StartsWith(search))
                                {
                                    list = true;
                                    break;
                                }
                            }
                        }
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            if (dictionary == true || list == true)
            {
                List<string> results = new List<string>();

                results = this.SearchForWords(temp, search, results);

                foreach (string s in results)
                {
                    searchResults.Add(result + s);
                }
            }
            return searchResults;
        }


        public List<string> SearchForWords(Node temp, string result, List<string> searchResults)
        {
            if (searchResults.Count < 10)
            {
                if (temp.eow == true)
                {
                    if (!searchResults.Contains(result))
                    {
                        searchResults.Add(result);
                    }
                    if (temp.list != null)
                    {
                        foreach (string title in temp.list)
                        {
                            if (searchResults.Count < 10)
                            {
                                if (title.StartsWith(result) || result.Equals(""))
                                {
                                    if (!searchResults.Contains(title))
                                    {
                                        searchResults.Add(title);
                                    }
                                }
                                else
                                {
                                    if (!searchResults.Contains(result + title))
                                    {
                                        searchResults.Add(result + title);
                                    }
                                }
                            }
                        }
                    }
                }

                if (temp.eow == false)
                {
                    if (temp.list != null)
                    {
                        if (temp.list.Count < 1)
                        {
                            if (!searchResults.Contains(result))
                            {
                                searchResults.Add(result);
                            }
                        }
                        else
                        {
                            foreach (string title in temp.list)
                            {
                                if (searchResults.Count < 10)
                                {
                                    if (title.StartsWith(result) || result.Equals(""))
                                    {
                                        if (!searchResults.Contains(title))
                                        {
                                            searchResults.Add(title);
                                        }
                                    }
                                    else
                                    {
                                        if (!searchResults.Contains(result + title))
                                        {
                                            searchResults.Add(result + title);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (temp.dictionary != null)
                {
                    foreach (var dictionary in temp.dictionary)
                    {
                        if (searchResults.Count >= 10)
                        {
                            break;
                        }
                        result += dictionary.Key;
                        Node newTemp = dictionary.Value;
                        this.SearchForWords(newTemp, result, searchResults);
                        result = result.Substring(0, result.Length - 1);
                    }
                }
            }
            return searchResults;
        }

    }
}
