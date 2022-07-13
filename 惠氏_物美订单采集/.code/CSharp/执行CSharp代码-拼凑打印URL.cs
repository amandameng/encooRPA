//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    string printURL = "https://pomanage.wumart.com/purchaseOrderVC/templates/VCprint/onePrint.html?";

    string cleanPrintUrl = string.Empty;
    string exceptionPrintUrl = string.Empty;
    
    List<string> cleanOrderNumJsonList = new List<string>{};
    List<string> exceptionOrderNumJsonList = new List<string>{};

    JArray jACleanColumns = new JArray() { };
    JArray jAExceptionColumns = new JArray() { };
    DataTable uniqOrdersDT = newOrdersDT.DefaultView.ToTable(true, new string[]{"订单号", "订单类型2"});

    int rowIndex = 1;
    foreach(DataRow dr in uniqOrdersDT.Rows){
        string 订单号 = dr["订单号"].ToString().Trim();
        string 订单类型 = dr["订单类型2"].ToString().Trim();
        
        if(cleanOrderList.Contains(订单号)){
            jACleanColumns.Add(itemObj(订单号, 订单类型));
        }
        
        if(exceptionOrderList.Contains(订单号)){
            jAExceptionColumns.Add(itemObj(订单号, 订单类型));
        }

        if(rowIndex % 20 == 0 ||  rowIndex ==uniqOrdersDT.Rows.Count ){
            if(jACleanColumns.Count > 0){
                JObject cleanOrderJObj = new JObject(
                                             new JProperty("isBatch", true),
                                             new JProperty("data", jACleanColumns)
                                        );
        
                Console.WriteLine(cleanOrderJObj.ToString());
                cleanPrintUrl = Uri.EscapeUriString(printURL + JsonConvert.SerializeObject(cleanOrderJObj));
                cleanPrintUrlList.Add(cleanPrintUrl);
                Console.WriteLine("cleanPrintUrl: {0}", cleanPrintUrl);
            }
        
            if(jAExceptionColumns.Count > 0){
                JObject exceptionOrderJObj = new JObject(
                                             new JProperty("isBatch", true),
                                             new JProperty("data", jAExceptionColumns)
                                        );
        
                Console.WriteLine(exceptionOrderJObj.ToString());
                exceptionPrintUrl = Uri.EscapeUriString(printURL + JsonConvert.SerializeObject(exceptionOrderJObj));
                exceptionPrintUrlList.Add(exceptionPrintUrl);
                Console.WriteLine("exceptionPrintUrl: {0}", exceptionPrintUrl);
            }
            // jACleanColumns初始化
            jACleanColumns = new JArray() { };
            jAExceptionColumns = new JArray() { };
    
        }
        rowIndex ++;
    }
    
   // Convert.ToInt32("as");

}
//在这里编写您的函数或者类

public JObject itemObj(string 订单号, string 订单类型){
    string finalType= string.Empty;
    if(订单类型 == "直流CPO"){
        finalType = "byShop";
    }else if(订单类型 == "直送PO"){
        finalType = "po";
    }
    JObject itemObj = new JObject(
                        new JProperty("mandt", "300"),
                        new JProperty("ebeln", 订单号),
                        new JProperty("history", false),
                        new JProperty("ptype", finalType));
    return itemObj;
}