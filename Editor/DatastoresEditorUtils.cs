using System.Collections.Generic;
using DatastoresDX.Runtime;

namespace DatastoresDX.Editor
{
    public static class DatastoresEditorUtils
    {
        public static bool DataElementPassesSearchString(IDataElement dataElement, string searchString)
        {
            if (string.IsNullOrEmpty(searchString))
            {
                return true;
            }
            
            List<string> searchArgs = new List<string>(searchString.ToLower().Split(' '));
            string sortString = dataElement.DisplayName.ToLower();

            foreach (string arg in searchArgs)
            {
                if (string.IsNullOrEmpty(arg))
                {
                    continue;
                }

                if (!sortString.Contains(arg))
                {
                    return false;
                }

                int index = sortString.IndexOf(arg);
                sortString = sortString.Remove(index, arg.Length);
            }
            
            return true;
        }
    }
}