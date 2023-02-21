//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    if(模块数据表.Columns.Contains("配置项")) 模块数据表.Columns["配置项"].ColumnName = "key";
    if(模块数据表.Columns.Contains("值"))  模块数据表.Columns["值"].ColumnName = "defaultValue";
    if(模块数据表.Columns.Contains("说明"))  模块数据表.Columns["说明"].ColumnName = "remark";

    string jsonStr = DataTableToJsonWithJsonNet(模块数据表);
    moduleConfigDic.Add(sheetName, jsonStr);
}


//在这里编写您的函数或者类
public static string DataTableToJsonWithJsonNet(DataTable table)
{
    string JsonString = string.Empty;
    JsonString = JsonConvert.SerializeObject(table);
    return JsonString;
}