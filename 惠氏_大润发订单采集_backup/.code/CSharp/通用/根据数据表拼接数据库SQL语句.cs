//代码执行入口，请勿修改或删除
public void Run()
{
    if (dbDataTable == null || dbDataTable.Rows.Count == 0)
    {
         return;
    }
    var columnsBuilder = new StringBuilder();
    foreach (DataColumn column in dbDataTable.Columns)
    {
        columnsBuilder.AppendFormat("{0},", column.ColumnName);
    }
    var strBuilder = new StringBuilder();
    foreach (DataRow row in dbDataTable.Rows)
    {
        var valuesBuilder = new StringBuilder();
        for (int i = 0; i < dbDataTable.Columns.Count; i++)
        {
            DataColumn columnInfo = dbDataTable.Columns[i];
            if (columnInfo.DataType == typeof(DateTime))
            {
                if (!string.IsNullOrEmpty(row[columnInfo.ColumnName].ToString()))
                {
                    valuesBuilder.AppendFormat("'{0}',", ((DateTime)row[columnInfo.ColumnName]).ToString("yyyy-MM-dd HH:mm:ss")); 
                }
                else
                {
                    //System.Console.WriteLine(string.Format("日期列 【{0}】无值", columnInfo.ColumnName));
                    valuesBuilder.Append("NULL,");      
                }
            }
            else if (columnInfo.DataType == typeof(int))
            {
                valuesBuilder.AppendFormat("{0},", row[columnInfo.ColumnName].ToString());
            }
            else
            {
                valuesBuilder.AppendFormat("'{0}',", row[columnInfo.ColumnName].ToString().Replace("'", "''").Replace("\\", "\\\\"));
            }  
        }
        if (!string.IsNullOrEmpty(columnsBuilder.ToString()))
        {
            strBuilder.AppendLine(string.Format("INSERT INTO `{0}` ({1}) VALUES ({2}){3}", 入库表名, columnsBuilder.ToString().TrimEnd(','), valuesBuilder.ToString().TrimEnd(','), ";"));
        }
    }
    str_SQL = strBuilder.ToString().Trim().TrimEnd(';');
    // System.Console.WriteLine(str_SQL);
}
//在这里编写您的函数或者类