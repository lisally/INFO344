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
        public Dictionary<char, Node> dictionary { get; private set; }
        public bool eow { get; set; }

        public Node()
        {
            this.dictionary = new Dictionary<char, Node>();
            this.eow = false;
        }
    }
}