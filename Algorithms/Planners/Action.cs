using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Algorithms.Planners
{
    public abstract class Action
    {
        public abstract IEnumerable<ResourceAmount> GetRequires(IEnumerable<ResourceAmount> state);
        public abstract IEnumerable<ResourceAmount> GetBorrows(IEnumerable<ResourceAmount> state);
        public abstract IEnumerable<ResourceAmount> GetConsumes(IEnumerable<ResourceAmount> state);
        public abstract IEnumerable<ResourceAmount> GetProduces(IEnumerable<ResourceAmount> state);
        public abstract TimeSpan GetDuration(IEnumerable<ResourceAmount> state);
    }
}
