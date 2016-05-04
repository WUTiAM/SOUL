using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

static class Utils
{
    public static System.Action<object> Log = Console.WriteLine;

    public static void ExitApplication(int code = 0)
    {
        Environment.Exit(0);
    }
}