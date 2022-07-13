//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    string mappingJson = File.ReadAllText(System.IO.Path.Combine(Environment.GetEnvironmentVariable("CurrentProjectSourcePath"),"Config", "excel_db_mapping.json"));
    
    // 作为参数
    // string str_TableName = "orders";
   Console.WriteLine(str_TableName);
   var mappingList = JsonConvert.DeserializeObject<List<MappingItem>>(mappingJson);

    MappingItem mappedTableItem = mappingList.FirstOrDefault(map => map.TableName.ToUpper().Equals(str_TableName.ToUpper()));
    if (mappedTableItem == null)
    {
        str_ErrorMessage = string.Format("Exit(0)：请先配置Excel与数据库表【{0}】的映射关系，路径为 .\\Config\\excel_db_mapping.json", str_TableName);
        return;
    }
    dbDataTable = GetDataBaseTable(mappedTableItem);
    var columnsBuilder = new StringBuilder();

    // 验证Excel 列和数据库字段是否全部匹配
    var invalidColumns = new List<string>();
    foreach (DataColumn column in 源数据表.Columns)
    {
        var fieldInfo = GetColumnInfoByDisplayName(mappedTableItem, column.ColumnName);
        if (fieldInfo == null)
        {
            invalidColumns.Add(column.ColumnName);
        }
    }
    if (invalidColumns.Any())
    {
        System.Console.WriteLine(string.Format("Excel文件中的列信息{0}在数据库表 {1} 中找不到对应的字段，存入数据库后该列信息将丢失", string.Join("、", invalidColumns.Select(col => "【" + col + "】").ToArray()), str_TableName));
    }
    // 将Excel的DataTable数据复制到 数据库的DataTable中
    foreach (DataRow row in 源数据表.Rows)
    {
        var dataRow = dbDataTable.NewRow();
        for (int i = 0; i < 源数据表.Columns.Count; i++)
        {
            DataColumn columnInfo = 源数据表.Columns[i];
            var fieldInfo = GetColumnInfoByDisplayName(mappedTableItem, columnInfo.ColumnName);

            if (fieldInfo != null)
            {
                if (fieldInfo.Type == "datetime" && !string.IsNullOrEmpty(row[columnInfo.ColumnName].ToString()))
                {
                    dataRow[fieldInfo.Field] = DateTime.Parse(row[columnInfo.ColumnName].ToString());
                }
                else if (fieldInfo.Type == "int")
                {
                    if (string.IsNullOrEmpty(row[columnInfo.ColumnName].ToString()))
                    {
                        if (fieldInfo.DisplayName != "主键")
                        {
                            dataRow[fieldInfo.Field] = 0;
                        }else{
                             源数据表.Columns.Remove(columnInfo);
                        }
                    }
                    else
                    {
                        dataRow[fieldInfo.Field] = int.Parse(row[columnInfo.ColumnName].ToString());
                    }
                }
                else
                {
                    dataRow[fieldInfo.Field] = row[columnInfo.ColumnName].ToString();
                }
            }
        }
        // 如果输出的数据库表结构字段多与Excel列数量，则需要在此进行添加

        dbDataTable.Rows.Add(dataRow);
    }
}

public DataTable GetDataBaseTable(MappingItem tableItem)
{
    var dataTable = new DataTable();
    try
    {
        foreach (var column in tableItem.Columns)
        {
            if (column.Type == "datetime" || column.Type == "timestamp")
            {
                if (column.Field != "created_time" && column.Field != "updated_time")
                {
                     dataTable.Columns.Add(column.Field, typeof(DateTime));
                }
               /* else
                {
                    // 创建时间默认为当前时间
                     var col = new DataColumn(column.Field, typeof(DateTime));
                     col.DefaultValue = DateTime.Now;
                     dataTable.Columns.Add(col);    
                }
                */
            }
            else if (column.Type == "int")
            {
                if (column.DisplayName != "主键")
                {
                     var col = new DataColumn(column.Field, typeof(int));
                     col.DefaultValue = 0;
                     dataTable.Columns.Add(col);
                }
            }
            else
            {
                dataTable.Columns.Add(column.Field, typeof(string));
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("根据映射关系生成数据表结构出错了，详情：" + ex.Message);
    }
    return dataTable;
}

public ColumnInfo GetColumnInfoByField(MappingItem tableItem, string fieldName)
{
    if (tableItem == null || tableItem.Columns == null)
    {
        return null;
    }

    return tableItem.Columns.FirstOrDefault(col => col.Field == fieldName);
}

public ColumnInfo GetColumnInfoByDisplayName(MappingItem tableItem, string displayName)
{
    if (tableItem == null || tableItem.Columns == null)
    {
        return null;
    }
    
    return tableItem.Columns.FirstOrDefault(col => col.DisplayName == displayName);
}


public class MappingItem
{
    [JsonProperty("name")]
    public string Name { get; set; }
    
    [JsonProperty("table_name")]
    public string TableName { get; set; }
    
    [JsonProperty("columns")]
    public List<ColumnInfo> Columns { get; set; }
}

public class ColumnInfo
{
    [JsonProperty("field")]
    public string Field { get; set; }
    
    [JsonProperty("type")]
    public string Type { get; set; }
    
    [JsonProperty("display_name")]
    public string DisplayName { get; set; }
}
//在这里编写您的函数或者类