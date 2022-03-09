//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    string finalCustomerName = orderDT.Rows[0]["customer_name"].ToString();
    Console.WriteLine(finalCustomerName);
    if(finalCustomerName == "沃尔玛"){
        setPdfForWMOrder();
    }else if(finalCustomerName.Contains("山姆")){
        setPdfForSamOrder(finalCustomerName);
    }else if(finalCustomerName == "沃尔玛IC"){
        walmartICFileName();
    }
    
}
//在这里编写您的函数或者类

// 沃尔玛订单文件路径
// 只下载散威化订单pdf, 命名规则：仓库代码＋订单号（渠道原始订单号）命名
public void setPdfForWMOrder(){
    if(bulkWalferConfigDT==null){
        return;
    }
    List<string> bulkWalferCodes = bulkWalferConfigDT.Rows.Cast<DataRow>().Select<DataRow, string>(dr => dr["customer_product_code"].ToString()).ToList();
    
    foreach(DataRow dr in orderItemsDT.Rows){
        string itemCode = dr["Item"].ToString();
        // 散威化代码
        if(bulkWalferCodes.Contains(itemCode)){
           isBulkOrder = true;
            break;
        }
    }
    if(isBulkOrder){
        string orderNumber = orderDT.Rows[0]["order_number"].ToString();
        Console.WriteLine("location: {0}", location);
        DataRow[] drs = curShipToDT.Select(String.Format("Nestle_Plant_No='{0}'", location));
        string wmdc = string.Empty;
        if(drs.Length > 0){
           wmdc = drs[0]["WMDC"].ToString();
        }
        
        string fileName = wmdc + orderNumber + ".pdf";
        pdfFilePath = System.IO.Path.Combine(pdfFolder, fileName); // C:\RPA工作目录\雀巢_沃尔玛\导出文件\订单pdf
    }
}

// 山姆订单文件路径
// 命名规则：SAM渠道+仓库代码＋订单号（渠道原始订单号）命名, 例如 SAM01-SZDC9050571100.pdf
public void setPdfForSamOrder(string customer_name){
    // DataTable 山姆主产品数据 = new DataTable();  // This should be passed as parameters
    string customerProductCode = orderItemsDT.Rows[0]["Item"].ToString();
    string distributionChannel = string.Empty;
     foreach(DataRow dr in orderItemsDT.Rows){
        string itemCode = dr["Item"].ToString();
        DataRow[] resultDRs = 山姆主产品数据.Select(string.Format("Customer_Material_No= '{0}' and Nestle_Plant_No='{1}'", itemCode, location));
        foreach(DataRow resultDR in resultDRs){
            string channel = resultDR["Distribution_Channel"].ToString();
            if(!String.IsNullOrEmpty(channel)){
                distributionChannel = channel;
                break;
            }
            if(!string.IsNullOrEmpty(distributionChannel)){
                break;
            }
        }
    }
    distributionChannel = String.IsNullOrEmpty(distributionChannel) ? "01" : distributionChannel;

    string orderNumber = orderDT.Rows[0]["order_number"].ToString();
    DataRow[] drs = curShipToDT.Select(String.Format("Nestle_Plant_No='{0}'", orderDT.Rows[0]["location"].ToString()));
    string wmdc = string.Empty;
    if(drs.Length > 0){
       wmdc = drs[0]["WMDC"].ToString();
    }
    string fileName = $"SAM{distributionChannel}-{wmdc}{orderNumber}.pdf";
    if(customer_name.Contains("Water")){
      fileName = $"SAM-IBU{distributionChannel}-{wmdc}{orderNumber}.pdf";
    }
    
    pdfFilePath = System.IO.Path.Combine(pdfFolder, fileName); // C:\RPA工作目录\雀巢_沃尔玛\导出文件\订单pdf
}

public void walmartICFileName(){
    string orderNumber = orderDT.Rows[0]["order_number"].ToString();
    DataRow[] drs = curShipToDT.Select(String.Format("Nestle_Plant_No='{0}'", orderDT.Rows[0]["location"].ToString()));
    string wmdc = string.Empty;
    if(drs.Length > 0){
       wmdc = drs[0]["WMDC"].ToString();
    }
    string mmdd = DateTime.Now.ToString("MMdd");
    // 4001014040-DGPDC-1021-1
    string fileName = $"{orderNumber}-{wmdc}-{mmdd}-{订单序号}.pdf";
    pdfFilePath = System.IO.Path.Combine(pdfFolder, fileName);
}