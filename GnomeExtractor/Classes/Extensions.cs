using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace GnomeExtractor
{
    public static class Extensions
    {
        // Охуеть, вместо наследования контрола, можно накатать ему расширение, о сколько нам открытий чудных
        // Extensions is nice solution instead inheritance
        public static string ToCSV(this DataTable table)
        {
            var result = new StringBuilder();
            for (int i = StaticValues.FirstColumnNames.Length - 1; i < table.Columns.Count; i++)
            {
                result.Append(table.Columns[i].ColumnName);
                result.Append(i == table.Columns.Count - 1 ? "\n" : ";");
            }

            foreach (DataRow row in table.Rows)
            {
                for (int i = StaticValues.FirstColumnNames.Length - 1; i < table.Columns.Count; i++)
                {
                    result.Append(row[i].ToString());
                    result.Append(i == table.Columns.Count - 1 ? "\n" : ";");
                }
            }

            return result.ToString();
        }
    }
}
