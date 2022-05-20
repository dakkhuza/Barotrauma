using Barotrauma.MoreLevelContent.Shared.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace MoreLevelContent.Shared.Store
{ 
    public abstract class StoreBase<T> : Singleton<T> where T : class
    {
    }
}
