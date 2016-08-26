using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApplebotEx.Modules
{
    public interface IBotPermissions
    {
        bool HasBotPermissions(object metadata);
    }
}
