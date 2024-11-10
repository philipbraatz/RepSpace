using Doorfail.Distribution.Network;
using Microsoft.AspNetCore.Mvc;

namespace Node_Network_Api.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class NodeController : ControllerBase, INodeRequests
    {
        private readonly INodeRequests _node;

        public NodeController(IHttpContextAccessor httpContextAccessor, Node? node = null)
        {
            var request = httpContextAccessor.HttpContext.Request;
            var nodeUri = new Uri($"{request.Scheme}://{request.Host}{request.PathBase}/api/Node");
            _node = node ?? new Node(nodeUri);
        }

        [HttpPost]
        public async Task ReceiveBlob([FromBody] Blob blob) => await _node.ReceiveBlob(blob);

        [HttpGet]
        public NodeInfo Info() => _node.Info();

        [HttpGet]
        public int ScriptCount() => _node.ScriptCount();

        [HttpPost]
        public async Task<ScriptGlobals> Run([FromBody] ScriptGlobals globals, [FromQuery] string[]? activateScripts = null)
            => await _node.Run(globals, activateScripts);

        [HttpPut]
        public void NextNode([FromBody] Uri newNextNodeUri) => _node.NextNode(newNextNodeUri);

        [HttpPost]
        public async Task BalanceScripts([FromBody] Blob blob) => await _node.BalanceScripts(blob);

    }
}
