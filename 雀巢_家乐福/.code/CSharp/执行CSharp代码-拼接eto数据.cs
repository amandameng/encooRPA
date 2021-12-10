//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    
    // 清空模板数据
    etoResultDT = 模板数据表.Clone();
    etoResultToDBDT = etoResultDT.Clone();
    etoResultToDBDT.Columns.Add("Customer Order Date");
    string globalOrderType = etoConfigDT.Rows[0]["order_type"].ToString();
    string globalSalesOrg = etoConfigDT.Rows[0]["sales_org"].ToString();
    string globalDisChannel = etoConfigDT.Rows[0]["distribution_channel"].ToString();
    string globalUom = etoConfigDT.Rows[0]["uom"].ToString();
    
    
    
    IEnumerable<IGrouping<string, DataRow>> groupedOrders = 联结查询数据表.Rows.Cast<DataRow>().GroupBy<DataRow, string>(dr => dr["order_number"].ToString());//C# 对DataTable中的某列分组，groupedDRs中的Key是分组后的值

    foreach (var itemGroup in groupedOrders)
    {
        DataRow[] orderItemRows = itemGroup.ToArray();
        string orderFirstBU = String.Empty;
        foreach(DataRow dr in orderItemRows){
            string curBU = dr["BU"].ToString().Trim();
            if(!string.IsNullOrEmpty(curBU)){
                orderFirstBU = curBU;
                break;
            }
        }
        
        foreach(DataRow dr in orderItemRows){
            DataRow etoRow = etoResultDT.NewRow();
            DataRow etoResultToDbRow = etoResultToDBDT.NewRow();
            etoRow["Order Type"] = globalOrderType;
            etoRow["Sales Org"] = globalSalesOrg;
            etoRow["Distribution channel"] = globalDisChannel;
            etoRow["Sold to"] = dr["sold_to_code"].ToString();
            etoRow["Ship to"] = dr["ship_to_code"].ToString();
            
            etoRow[7] = dr["line_number"].ToString(); // PO: 2 位于excel第8列
            
            etoRow["PO Number"] = combinePONumber(dr, orderFirstBU);
            etoRow["Reqd Del Date"] = DateTime.Parse(dr["request_delivery_date"].ToString()).ToString("yyyyMMdd");
            etoRow["SAP Material"] = dr["雀巢产品编码"].ToString();
    
            string 订单箱规 = dr["package_size"].ToString();
            string 订单箱规数字字符 = 获取订单箱规(订单箱规);
        
            decimal 产品确认数量 = Convert.ToDecimal(dr["confirm_qty"].ToString());
            decimal 订单箱规数字 = Convert.ToDecimal(订单箱规数字字符);
            decimal 箱数= 产品确认数量 / 订单箱规数字;
            
            etoRow["Qty"] = 箱数;
            etoRow["UoM"] = globalUom;
    
            etoResultDT.Rows.Add(etoRow);
            
            etoResultToDbRow.ItemArray = etoRow.ItemArray;
            etoResultToDbRow["Customer Order Date"] = dr["create_date"]; // !!! not in excel template
            etoResultToDBDT.Rows.Add(etoResultToDbRow);
        }
    }
}
//在这里编写您的函数或者类
// 采购单号+订单类型缩写（越库-YK，昆山花桥1仓-HQ，存货-CH）订单中含有NPP产品需要再加上NPP，即为：采购单号+订单类型缩写+NPP
public string combinePONumber(DataRow dr, string orderFirstBU){
    string poNumber = dr["order_number"].ToString();
    string 仓库地址 = dr["logistics_warehouse"].ToString(); // 对应订单里面的仓库地址
    string 仓库短码 = dr["order_type_short"].ToString(); // 对应 ship to sold to 表里面的字段
   // string nppStr = dr["BU"].ToString(); // 客户-雀巢主产品表里面的字段
    string nppStr = String.IsNullOrEmpty(dr["BU"].ToString()) ? orderFirstBU : dr["BU"].ToString(); // 客户-雀巢主产品表里面的字段

    string order_type = dr["order_type"].ToString();// order_type
    if(String.IsNullOrEmpty(仓库短码)){
       仓库短码 = "CH"; // 默认CH
    }
    if(order_type.Contains("越库")){
       仓库短码 = "YK"; 
    }
    return String.Format("{0}{1}{2}", poNumber, 仓库短码, nppStr.ToUpper() == "NPP" ? "NPP" : "");
}

public string 获取订单箱规(string 订单箱规){
    Regex 数字正则 = new Regex(@"(\d+)");
    Match matchResult = 数字正则.Match(订单箱规);
    string 箱规数字 = matchResult.Value;
    return 箱规数字;
}