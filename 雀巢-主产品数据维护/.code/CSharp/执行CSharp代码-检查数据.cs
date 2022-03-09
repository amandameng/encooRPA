//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    异常数据表 = allDataDT.Clone();
    新增数据表 = allDataDT.Clone();
    updateSqlList = new List<string>{};
    异常数据表.Columns.Add("异常原因", typeof(string));
    DateTime timeNow = DateTime.Now;
    string timeNowStr = timeNow.ToString("yyyy-MM-dd HH-mm-ss");
    /*
    读取特殊产品，沃尔玛和山姆产品存在客户码1:n 雀巢码的问题，所以需要排除这些产品
    */
    DataTable 特殊产品数据表 = searchData("select * from nestle_bulk_walfer_config");
    
    List<string> 特殊客户产品码列表 = 特殊产品数据表.Rows.Cast<DataRow>().Select<DataRow, string>(dr => dr["customer_product_code"].ToString()).ToList();
    
    if(allDataDT != null && allDataDT.Rows.Count > 0){
        foreach(DataRow dr in allDataDT.Rows){
            string 修改类型 = dr["Modify Type"].ToString().Trim();
            string shipTo = dr["Ship to"].ToString().Trim();
            string plantNo = dr["Nestle Plant No"].ToString().Trim();
            string 客户产品码 = dr["Customer Material No"].ToString().Trim();
            string 雀巢产品码 = dr["Nestle Material No"].ToString().Trim();
            string 渠道名称 = dr["Customer Name"].ToString().Trim();
            // 如果是特殊1:N产品，则加入异常订单
            if(特殊客户产品码列表.Contains(客户产品码)){
                addExceptionRow(ref 异常数据表, "此客户产品码对应雀巢多条产品码，跳过", dr);
                continue;
            }
            /*
            新增
            修改雀巢编码
            修改客户编码
            修改其他
            */

            if(修改类型 == ModifyType.新增.ToString()){
                string searchQuery = string.Format("select * from `material_master_data` where Customer_Name='{0}' and Ship_to_Code='{1}' and Nestle_Plant_No='{2}' and Customer_Material_No='{3}' and Nestle_Material_No = '{4}'", 渠道名称, shipTo, plantNo, 客户产品码, 雀巢产品码);
                DataTable existingDT = searchData(searchQuery);
                // 此条记录已经存在
                if(existingDT.Rows.Count > 0){
                    addExceptionRow(ref 异常数据表, "此条记录已经存在，跳过", dr);
                    continue;
                }else{
                    dr["Modify Type"] = ModifyType.新增.ToString();
                    新增数据表.ImportRow(dr);
                }
            }else if(修改类型 == ModifyType.修改客户编码.ToString()){
                string searchQuery = string.Format("select * from `material_master_data` where Customer_Name='{0}' and Ship_to_Code='{1}' and Nestle_Plant_No='{2}' and Nestle_Material_No = '{3}'", 渠道名称, shipTo, plantNo, 雀巢产品码);
                DataTable existingDT = searchData(searchQuery);
                // 当前数据记录条数唯一
                if(existingDT.Rows.Count == 1){
                    string updateSql = string.Format("update `material_master_data` set Customer_Material_No='{0}', Update_Time='{5}' where Customer_Name='{1}' and Ship_to_Code='{2}' and Nestle_Plant_No='{3}' and Nestle_Material_No = '{4}' and Modify_Type='{5}'",
                        客户产品码, 渠道名称, shipTo, plantNo, 雀巢产品码, timeNowStr, ModifyType.修改客户编码.ToString());
                    Console.WriteLine("11111: {0}", updateSql);
                    updateSqlList.Add(updateSql);
                }else if(existingDT.Rows.Count == 0){
                    addExceptionRow(ref 异常数据表, "此条记录不存在，故无法{ModifyType.修改客户编码.ToString()}，跳过", dr);
                }else if(existingDT.Rows.Count > 1){
                    addExceptionRow(ref 异常数据表, "此雀巢产品码对应多条客户产品码，跳过", dr);
                }
            }else if(修改类型 == ModifyType.修改雀巢编码.ToString()){
                string searchQuery = string.Format("select * from `material_master_data` where Customer_Name='{0}' and Ship_to_Code='{1}' and Nestle_Plant_No='{2}' and Customer_Material_No = '{3}'", 渠道名称, shipTo, plantNo, 客户产品码);
                DataTable existingDT = searchData(searchQuery);
                // 当前数据记录条数唯一
                if(existingDT.Rows.Count == 1){
                    string updateSql = string.Format("update `material_master_data` set Nestle_Material_No='{0}', Update_Time='{5}' where Customer_Name='{1}' and Ship_to_Code='{2}' and Nestle_Plant_No='{3}' and Customer_Material_No = '{4}' and Modify_Type='{5}'", 
                        雀巢产品码, 渠道名称, shipTo, plantNo, 客户产品码, timeNowStr, ModifyType.修改雀巢编码.ToString());
                                   Console.WriteLine("2222: {0}", updateSql);

                    updateSqlList.Add(updateSql);
                }else if(existingDT.Rows.Count == 0){
                    addExceptionRow(ref 异常数据表, $"此条记录不存在，故无法{ModifyType.修改雀巢编码.ToString()}，跳过", dr);
                }else if(existingDT.Rows.Count > 1){
                    addExceptionRow(ref 异常数据表, "此客户产品码对应多条雀巢产品码，跳过", dr);
                }
            }else if(修改类型 == ModifyType.修改其他.ToString()){
                string searchQuery = string.Format("select * from `material_master_data` where Customer_Name='{0}' and Ship_to_Code='{1}' and Nestle_Plant_No='{2}' and Customer_Material_No='{3}' and Nestle_Material_No = '{4}'", 渠道名称, shipTo, plantNo, 客户产品码, 雀巢产品码);
                DataTable existingDT = searchData(searchQuery);
                // 此条记录已经存在
                if(existingDT.Rows.Count > 0){
                    
                    string[] otherColumns = new string[]{"Material_Description","Nestle_BU","Nestle_NPS", "Nestle_Case_Configuration", "Adjustive_Price", "Tax_Point"};
                    Dictionary<string, string> colMapingDic = new Dictionary<string,string>{
                        {"Material_Description", "Material Description"},
                        {"Nestle_BU", "Nestle BU"},
                        {"Nestle_NPS", "Nestle NPS"},
                        {"Nestle_Case_Configuration", "Nestle Case Configuration"},
                        {"Adjustive_Price", "Adjustive Price"},
                        {"Tax_Point", "Tax Point"}
                    };
                    List<string> updateColList = new List<string>{};
                    foreach(var colDic in colMapingDic){
                        updateColList.Add(string.Format("{0}='{1}'", colDic.Key, dr[colDic.Value].ToString()));
                    }
                    updateColList.Add(string.Format("Update_Time='{0}'", timeNowStr));
                    updateColList.Add(string.Format("Modify_Type='{0}'", ModifyType.修改其他.ToString()));
                   
                    string updateSql = string.Format("update `material_master_data` set {0} where Customer_Name='{1}' and Ship_to_Code='{2}' and Nestle_Plant_No='{3}' and Customer_Material_No = '{4}'  and Nestle_Material_No = '{5}'",
                    string.Join(", ", updateColList), 渠道名称, shipTo, plantNo, 客户产品码, 雀巢产品码);
                    Console.WriteLine("3333: {0}", updateSql);

                    updateSqlList.Add(updateSql);
                }else{
                    addExceptionRow(ref 异常数据表, $"此条记录不存在，故无法{ModifyType.修改其他.ToString()}，跳过", dr);
                }
            }
        }
    }
}
//在这里编写您的函数或者类

public void addExceptionRow(ref DataTable 异常数据表ref, string msg, DataRow dr){
    DataRow 异常数据行 = 异常数据表ref.NewRow();
    异常数据行.ItemArray = dr.ItemArray;
    异常数据行["异常原因"] = msg; // "此客户码对应雀巢多条产品码，不做处理";
    异常数据表ref.Rows.Add(异常数据行);
}

public enum ModifyType
{
    新增,
    修改雀巢编码,
    修改客户编码,
    修改其他,
}

    
public DataTable searchData(string queryStr)
{
    System.Data.DataTable table = new System.Data.DataTable();
    using (MySqlConnection connection = new MySqlConnection(sqlConn))
    {
        MySqlCommand cmd = connection.CreateCommand();
        cmd.CommandText = queryStr;
        connection.Open();
        MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
        adapter.Fill(table);
        connection.CloseAsync().Wait();
    }
    return table;
}