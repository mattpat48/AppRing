using Ring.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ring.Shared
{
    public interface IMyHttpClient
    {
        HttpClient sharedClient { get; set; }
        MainAPI sharedMainAPI { get; set; }
    }
}
