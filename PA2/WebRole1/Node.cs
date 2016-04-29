using System.Collections.Generic;
/// <summary>
/// Node class for the Trie data structure
/// Contains a dictionary and a boolean value to separate leaf nodes
/// from tree nodes.
/// </summary>
namespace WebRole1
{
    public class Node
    {
        public Dictionary<char, Node> dictionary { get; set; }

        public bool eow { get; set; }

        public Node()
        {
            this.dictionary = null;
            this.eow = false;
        }
    }
}