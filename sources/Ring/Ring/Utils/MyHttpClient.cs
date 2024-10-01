using Ring.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ring.Utils;
internal class MyHttpClient : IMyHttpClient
{
    public HttpClient sharedClient { get; set; }
    public MainAPI sharedMainAPI { get; set; }
}
