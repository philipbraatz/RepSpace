using System.Security.Cryptography;
using Doorfail.Distribution.Network;

var nodeCount = 8;
List<Node> nodes = [];

// Create initial nodes with optional names
for (int i = 0; i < nodeCount; i++)
{
    Node node = new($"Node-{i}");
    nodes.Add(node);
    if (i > 0)
    {
        nodes[i - 1].NextNode =node; // Link to the next node
    }
}

// Create a circular link (the last node points to the first)
nodes[^1].NextNode = nodes[0];

// Prepare scripts for initial blob
var scripts = new Dictionary<RSAParameters, Script>
{
        { nodes[0].GetPublicKey(), new Script("Json Script", @"
                using Newtonsoft.Json;
                var data = new { Message = $""Hello from Node {NodeId}"" };
                return JsonConvert.SerializeObject(data);
            ")
        {
            Packages =
            [ new( "Newtonsoft.Json")
                { Version = "13.0.3" }
            ],
            Imports = [ "Newtonsoft.Json" ]
        }
    },
    { nodes[1].GetPublicKey(), new Script ("Script2", "return $\"World from Node {NodeId}.\";" ) },
    { nodes[2].GetPublicKey(), new Script ("Script3", "return $\"Eppy from Node {NodeId}.\";" )},
    { nodes[3].GetPublicKey(), new Script ("Script4", "return $\"Sleepy from Node {NodeId}.\";" ) },
    { nodes[4].GetPublicKey(), new Script ("Script5", "return $\"Frodo from Node {NodeId}.\";" ) },
    { nodes[5].GetPublicKey(), new Script ("Script6", "return $\"Gandalf from Node {NodeId}.\";" )},
    { nodes[6].GetPublicKey(), new Script ("Script7", "return $\"Aragorn from Node {NodeId}.\";" ) },
    { nodes[7].GetPublicKey(), new Script ("Script8", "return $\"Legolas from Node {NodeId}.\";" ) }
};

// Send initial blob from a random node
var randomNode = nodes[Random.Shared.Next(nodeCount)];
randomNode.SendBlob(scripts);

// Initiate aggregation from a random node
Console.WriteLine(await randomNode.AggregateAsync(start: true));

// Demonstration of inserting a new node in the circular list
Node newNode = new ($"Node-{nodeCount}");
var randomExistingNode = nodes[Random.Shared.Next(nodeCount)];
randomExistingNode.InsertNodeAfter(newNode);
Console.WriteLine($"Inserted new Node {newNode.GetPublicKey()} after Node {randomExistingNode.GetPublicKey()}.");

// Update the list of nodes to include the new node
nodes.Add(newNode);
newNode.NextNode =randomExistingNode.NextNode; // Set new node's next to existing node's next
randomExistingNode.NextNode = newNode; // Update the current node's next to point to the new node

Console.WriteLine("Press any key to exit...");
Console.ReadKey();