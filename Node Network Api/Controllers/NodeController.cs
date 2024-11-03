using Doorfail.Distribution.Network;
using Microsoft.AspNetCore.Mvc;

namespace Node_Network_Api.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class NodeController : ControllerBase, INodeRequests
    {
        private readonly INodeRequests _node;

        public NodeController(IHttpContextAccessor httpContextAccessor)
        {
            var request = httpContextAccessor.HttpContext.Request;
            var nodeUri = new Uri($"{request.Scheme}://{request.Host}{request.PathBase}/api/Node");
            _node = new Node(nodeUri);
        }

        [HttpPost]
        public async Task ReceiveBlob([FromBody] Blob blob)
        {
            await _node.ReceiveBlob(blob);
        }

        [HttpGet]
        public NodeInfo Info()
        {
            return _node.Info();
        }

        [HttpGet]
        public int ScriptCount()
        {
            return _node.ScriptCount();
        }

        [HttpPost]
        public async Task<ScriptGlobals> Run([FromBody] ScriptGlobals globals)
        {
            return await _node.Run(globals);
        }

        [HttpPut]
        public void NextNode([FromBody] Uri newNextNodeUri)
        {
            _node.NextNode(newNextNodeUri);
        }

    }
}
