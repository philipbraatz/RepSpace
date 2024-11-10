using Doorfail.Distribution.Network;
using Microsoft.AspNetCore.Mvc;

namespace Node_Network_Api.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class RegionalNodeController : ControllerBase, INodeRequests
    {
        private readonly RegionalNode _regionalNode;

        public RegionalNodeController(IHttpContextAccessor httpContextAccessor, Uri regionalNextNodeUri, Node? node = null)
        {
            var request = httpContextAccessor.HttpContext.Request;
            var nodeUri = new Uri($"{request.Scheme}://{request.Host}{request.PathBase}/api/RegionalNode");
            _regionalNode = new RegionalNode(nodeUri, regionalNextNodeUri, node?.NextNodeUri);
        }

        [HttpPost]
        public async Task ReceiveBlob([FromBody] Blob blob) => await _regionalNode.ReceiveBlob(blob);

        [HttpGet]
        public NodeInfo Info() => _regionalNode.Info();

        [HttpGet]
        public int ScriptCount() => _regionalNode.ScriptCount();

        [HttpPost]
        public async Task<ScriptGlobals> Run([FromBody] ScriptGlobals globals, [FromQuery] string[]? activateScripts = null)
            => await _regionalNode.Run(globals, activateScripts);

        [HttpPut]
        public void NextNode([FromBody] Uri newNextNodeUri) => _regionalNode.NextNode(newNextNodeUri);

        [HttpPost]
        public async Task BalanceScripts([FromBody] Blob blob) => await BalanceRegionalScripts(blob);

        [HttpGet]
        public async Task<int> RegionalScriptCount() => await _regionalNode.RegionalScriptCount();

        [HttpPost]
        public async Task TransferScriptsToRegion([FromBody] Blob blob) => await _regionalNode.TransferScriptsToRegion(blob);

        [HttpGet]
        public async Task<int> GetNextRegionalNodeScriptCount() => await _regionalNode.GetNextRegionalNodeScriptCount();

        [HttpPost]
        public async Task BalanceRegionalScripts([FromBody] Blob blob) => await _regionalNode.TransferScriptsToRegion(blob);
    }
}
