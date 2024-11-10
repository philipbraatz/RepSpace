﻿using Doorfail.Distribution.Network;

namespace Node_Network_Api.Controllers;

public interface INodeRequests
{
    NodeInfo Info();
    int ScriptCount();
    Task<ScriptGlobals?> Run(ScriptGlobals globals, string[] activateScripts = null);
    Task ReceiveBlob(Blob blob);

    void NextNode(Uri newNextNodeUri);
    Task BalanceScripts(Blob blob);
}