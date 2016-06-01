using System.Collections.Generic;

namespace Library
{
    public class Node
    {
        public List<string> list { get; set; }
        public Dictionary<char, Node> dictionary { get; set; }

        public bool eow { get; set; }

        public Node()
        {
            this.list = null;
            this.dictionary = null;
            this.eow = false;
        }
    }
}