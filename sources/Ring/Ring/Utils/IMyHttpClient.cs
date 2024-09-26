using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ring.Utils;
public interface IMyHttpClient
{
    HttpClient sharedClient { get; set; }
}
