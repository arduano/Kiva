using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KivaShared
{
    class InstallFailedException : Exception
    {
        public InstallFailedException(string message) : base(message) { }
    }
}
