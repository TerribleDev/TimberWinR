using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace TimberWinR.Diagnostics
{
    public interface IDiagnosable
    {
        JObject ToJson();
    }
}
