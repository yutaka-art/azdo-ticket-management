using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace CspFoundation.Commons
{
    public class MethodHelper
    {
        public static string GetCurrentMethod([CallerMemberName] string callerName = "")
        {
            var stackTrace = new StackTrace();
            var frame = stackTrace.GetFrame(1); // 0はGetCurrentMethod自身、1は呼び出し元のメソッド
            var method = frame.GetMethod();
            var className = method.DeclaringType?.FullName;

            if (className != null)
            {
                int index = className.IndexOf('+');
                if (index != -1)
                {
                    className = className.Substring(0, index);
                }
            }

            return $"{className}.{callerName}";
        }
    }
}
