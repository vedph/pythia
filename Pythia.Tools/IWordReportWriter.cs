using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pythia.Tools;

public interface IWordReportWriter
{
    void Open(string target);
    void Write(WordCheckResult result);
    void Close();
}
