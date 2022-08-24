//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    string mappingJson = File.ReadAllText(System.IO.Path.Combine(Environment.GetEnvironmentVariable("CurrentProjectSourcePath"),"Config", "excel_db_mapping.json"));
    // printDT(源数据表);
    // 作为参数
    // string str_TableName = "orders";
    var mappingList = JsonConvert.DeserializeObject<List<MappingItem>>(mappingJson);
    MappingItem mappedTableItem = mappingList.FirstOrDefault(map=> map.TableName.Equals(str_TableName));
    if (mappedTableItem == null)
    {
         throw new Exception(string.Format("请先配置excel结果文件与数据库表{0}对应的表映射关系，路径为 .\\Config\\excel_db_mapping.json", str_TableName));
    }
    
    JArray mappingArr = JsonConvert.DeserializeObject<JArray>(mappingJson);
    System.Text.StringBuilder sqlBuilder = new System.Text.StringBuilder();
    foreach(JToken token in mappingArr.Where(t => t.Value<string>("table_name") == str_TableName).ToList())
    {
        var missedFields=string.Empty;
        if(insertIgnore){
            sqlBuilder.Append($"insert ignore into `{str_TableName}`(");
        }else{
            sqlBuilder.Append($"insert into `{str_TableName}`(");  
        }

        JArray columnArr = token.Value<JArray>("columns");
        //生成insert列名
        List<string> columnNames = new List<string>{};
        //foreach(JToken jtok in columnArr){
            //Console.WriteLine("----jtok----{0}", jtok.Value<string>("display_name"));
        //}
        foreach(DataColumn dc in 源数据表.Columns)
        {
            //Console.WriteLine("----dc.ColumnName----{0}", dc.ColumnName);
            List<JToken> columnsMapping = columnArr.Where(t => t.Value<string>("display_name") == dc.ColumnName).ToList();
            
            //Console.WriteLine("----columnsMapping.Count----{0}", columnsMapping.Count);
            
            if(columnsMapping.Count == 1)
            {
                columnNames.Add("`" + columnsMapping[0].Value<string>("field") + "`");
            }
           else{ //  if(columnsMapping.Count > 1)
              int intCol;
              bool isNumber = Int32.TryParse(dc.ColumnName, out intCol);
               if(!isNumber){
                   Console.WriteLine("-mapping count 不等于1-{0}--", dc.ColumnName);
                   missedFields=missedFields+"["+dc.ColumnName+"],";
               }
            }
        }
         sqlBuilder.Append(String.Join(",", columnNames));
        
       if (!string.IsNullOrWhiteSpace(missedFields))
       {
           string errorMsg = "Excel字段【"+missedFields.Trim(',')+"】缺少与数据库表的映射关系，"+string.Format("请检查并更新Excel与数据库表【{0},{1}】的映射关系，路径为 .\\Config\\excel_db_mapping.json",mappedTableItem.Name, str_TableName);
           throw(new Exception(errorMsg));
       }

        //追加字段
        sqlBuilder.Append(",created_time");
        sqlBuilder.Append(") values ");

        //生成insert value值
        foreach(DataRow dr in 源数据表.Rows)
        {
            sqlBuilder.Append("(");
            foreach(DataColumn dc in 源数据表.Columns)
            {
               // Console.WriteLine("dc.ColumnName {0}", dc.ColumnName);
                List<JToken> columnsMapping = columnArr.Where(t => t.Value<string>("display_name") == dc.ColumnName).ToList();
                
                if(columnsMapping.Count == 1)
                {
                    string cellValue = dr[dc.ColumnName].ToString();
                    string columnType = columnsMapping[0].Value<string>("type");
                    if(!String.IsNullOrEmpty(cellValue)){
                        if(columnType.Contains("decimal")){
                            cellValue = Convert.ToDecimal(cellValue).ToString();
                        }else if(columnType=="date" && !cellValue.Contains("-") && !cellValue.Contains("/")){
                            // Console.WriteLine("---------cellValue:{0}", cellValue);
                            cellValue = DateTime.ParseExact(cellValue, "yyyyMMdd", null).ToString("yyyy-MM-dd");
                        }else if(columnType=="mdy_date"){
                            cellValue = DateTime.ParseExact(cellValue, "MM/dd/yyyy", null).ToString("yyyy-MM-dd");
                        }else if(columnType=="datetime"){
                            cellValue = Convert.ToDateTime(cellValue).ToString();
                        }
                       // Console.WriteLine("cellValue, {0}", cellValue);
                        sqlBuilder.Append("'" + cellValue + "',");
                    }else{
                      sqlBuilder.Append("null,");
                    }
                }
            }
            sqlBuilder.Append("'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "'");
            sqlBuilder.Append("),");
        }
        sqlBuilder = sqlBuilder.Remove(sqlBuilder.Length - 1, 1);
    }
    str_SQL = string.Format(@"{0}", sqlBuilder.ToString());
   // System.Console.WriteLine(str_SQL);
    
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

/// <summary>
/// 辅助打印数据表方法
/// </summary>
/// <param name="theDT"></param>
public void printDT(DataTable theDT){
    DataColumnCollection dcols = theDT.Columns;
    foreach(DataRow dr in theDT.Rows){
        foreach(DataColumn dc in dcols){
            Console.WriteLine("column:{0}, value:{1}", dc.ColumnName, dr[dc.ColumnName]);
        }
        Console.WriteLine("---------------------------------------");
    }
}