using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TimberWinR.Inputs;

namespace TimberWinR
{
    public interface ICodec
    {
        void Apply(string msg, InputListener listener);
    }
}
