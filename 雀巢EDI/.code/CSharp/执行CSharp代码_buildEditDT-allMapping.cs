/*
不完整字段也转EDI，除了Qty为0或者空的Item，如果为空则不放入EDI
*/
//代码执行入口，请勿修改或删除
public string ediSendFolder;
public string ediSentFolder;
public const string SEND_NAME = "Send";
public const string SENT_NAME = "Sent";


public void Run()
{
    //在这里编写您的代码
    //try{
        initEdiDT();
        initExceptionOrdersDT();
        buildEdiData();
      
        string editFileName = "edi_" + timeNowTicks + ".csv";
        ediSendFolder  = System.IO.Path.Combine(connectionFolder, SEND_NAME);
        ediSentFolder  = System.IO.Path.Combine(connectionFolder, SENT_NAME);
        string ediFilePath = System.IO.Path.Combine(ediSendFolder, editFileName);
        string resultEdiFilePath = ediDataIntoFile(ediFilePath);

        // 文件不为空，则检查数据状态，文件是否进Sent文件夹
        if(!string.IsNullOrEmpty(resultEdiFilePath)){
            ediFileSentPath = System.IO.Path.Combine(ediSentFolder, editFileName);
            ediStatus = checkFileStatus(ediFileSentPath); // 检查文件是否发送成功
            ediMessage = ediStatus ? "Edi Success" : "EDI File Sent Time Out";
        }else{
            ediStatus = true;
            ediMessage = "no data";  // EDI process successfully but no data
        }
  //  }catch(Exception e){
   //     ediStatus = false;
   //     ediMessage = e.Message; // EDI process failed
        // Convert.ToInt16("as");
   // }

    // 分客户发异常提醒
     customerExceptionDTDic = new Dictionary<string, DataTable>();
     customerExceptionDTReasonDic = new Dictionary<string, DataTable>();
    
    // 收集异常订单，整理成EX2O模板格式
    if(allExceptionOrdersDT!= null && allExceptionOrdersDT.Rows.Count > 0){
        DataTable allExceptionEx2ODT = allToDoEx2oDT.Clone();
        foreach(DataRow dr in allExceptionOrdersDT.Rows){
            string order_number = dr["order_number"].ToString();
            DataRow[] matchedDrs = allToDoEx2oDT.Select("Customer_Order_Number='" + order_number +"'");
            foreach(DataRow marchedDr in matchedDrs){
                allExceptionEx2ODT.ImportRow(marchedDr);
            }
        }
        // allExceptionEx2ODT = allToDoEx2oDT.Clone();
        
        if(allExceptionEx2ODT.Columns.Contains("id")){
            allExceptionEx2ODT.PrimaryKey = null;
            allExceptionEx2ODT.Columns.Remove("id");
        }
        
        foreach(DataRow cusRow in validEdiCustomerDT.Rows){  // 遍历 active suctomer name
            string customer_name = cusRow["customer_name"].ToString();
            DataTable customerExceptionEX2ODT = allExceptionEx2ODT.Clone();
            DataTable customerExceptionOrdersDT = allExceptionOrdersDT.Clone();
            DataRow[] customerDrs = allExceptionEx2ODT.Select("Customer_Name = '" + customer_name + "'");
            foreach(DataRow dr in customerDrs){
                customerExceptionEX2ODT.ImportRow(dr);
                string order_number = dr["Customer_Order_Number"].ToString();
                DataRow[] matchedDrs = allExceptionOrdersDT.Select("order_number='" + order_number +"'");
                foreach(DataRow matchedExceptionReasonDR in matchedDrs){
                    DataRow[] existingDRs = customerExceptionOrdersDT.Select("order_number='" + order_number +"'");
                    if(existingDRs.Length == 0){
                        customerExceptionOrdersDT.ImportRow(matchedExceptionReasonDR);
                    }
                }
            }
            if(customerExceptionEX2ODT.Rows.Count > 0){
                customerExceptionDTDic[customer_name] = customerExceptionEX2ODT;
                customerExceptionDTReasonDic[customer_name] = customerExceptionOrdersDT;
            }
        }

    }
}
//在这里编写您的函数或者类

/*

H	Order Type	Sales Org	Distribution channel	Sold to	Sold to Name	Ship to	Ship to Name	PO Number	Reqd Del Date	Delivery note text	Language	Order Reason	Cost Center	OTC Name	OTC Street	OTC City Name	OTC Check	Spec.stock Partner	Header Assignment	Header Reference
L	SAP Material	SAP Description	Qty	UoM	SLoc	Batch	Plant	WBS	Item Category	Route	Item Condition Type	Item Condition Value	Item condition type 1	Item Condition Value 1						
L	SAP Material	SAP Description	Qty	UoM	SLoc	Batch	Plant	WBS	Item Category	Route	Item Condition Type	Item Condition Value	Item condition type 1	Item Condition Value 1						

*/
public void initEdiDT(){
    ediDT = new DataTable();
    string[] columnsArr = new string[]{"A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U"};
    foreach(string item in columnsArr){
       ediDT.Columns.Add(item, typeof(string)); 
    }
}

public void initExceptionOrdersDT(){
    allExceptionOrdersDT = new DataTable();
    string[] columnsArr = new string[]{"order_number", "message"};
    foreach(string item in columnsArr){
       allExceptionOrdersDT.Columns.Add(item, typeof(string)); 
    }
}

public void  updateOrigEX2OSTST(DataRow newEdiOrderRow){
    foreach(DataRow dr in allToDoEx2oDT.Rows){
        string ediOrderNumber = newEdiOrderRow["I"].ToString();
        if(dr["PO_Number"].ToString() == ediOrderNumber){
            dr["Sold_to"] = newEdiOrderRow["E"];
            dr["Ship_to"] = newEdiOrderRow["G"];
        }
    }
}

public void  updateOrigEX2OSapMaterial(DataRow newEdiOrderItemRow, string order_number, string sapMaterial){
    // Console.WriteLine("order_number: {0}, sapMaterial: {1}", order_number, sapMaterial);
    foreach(DataRow dr in allToDoEx2oDT.Rows){
        // Console.WriteLine("Customer_Order_Number: {0}, SAP_Material: {1}", dr["Customer_Order_Number"], dr["SAP_Material"]);
        if(dr["Customer_Order_Number"].ToString() == order_number &&  sapMaterial == dr["SAP_Material"].ToString() ){
            dr["SAP_Material"] = newEdiOrderItemRow["B"].ToString();
        }
    }
}

public void buildEdiData(){
    IEnumerable<IGrouping<string, DataRow>> ex2oGroupByOrders = allToDoEx2oDT.Rows.Cast<DataRow>().GroupBy<DataRow, string>(dr => dr["Customer_Order_Number"].ToString());

    foreach (var itemGroup in ex2oGroupByOrders)
    {
        string orderNumber = itemGroup.Key;
        DataRow[] groupedDrs= itemGroup.ToArray();
        DataRow orderRow = groupedDrs[0];
        List<string> orderExceptionFieldsList = new List<string>{};
        List<string> orderDataInvalidFieldsList = new List<string>{};
        string orderExceptionMessage = "";
       // bool ifOrderIntoEDI = true;
        DataRow newEdiOrderRow = buildEdiOrderRow(orderRow, ref orderExceptionFieldsList, ref orderDataInvalidFieldsList);
        if(!is_edi_production) updateOrigEX2OSTST(newEdiOrderRow); // 更新原始订单为测试ship to, sold to
        
        // 如果数据完整则检查Item
        List<DataRow> orderItemRowsList = new List<DataRow>{};

        foreach(DataRow dRow in groupedDrs){
            bool intoEdi = true;
            DataRow newEdiOrderItemRow = buildEdiOrderItemRow(dRow, ref orderExceptionFieldsList, ref orderDataInvalidFieldsList, ref intoEdi);
            if(!is_edi_production) updateOrigEX2OSapMaterial(newEdiOrderItemRow, orderNumber, dRow["SAP_Material"].ToString()); // 更新原始订单为测试物料号
            if(intoEdi){
                orderItemRowsList.Add(newEdiOrderItemRow);
            }
        }

        if(orderItemRowsList.Count != 0){  // Items 不为空
            ediDT.Rows.Add(newEdiOrderRow); // 只有Items 不为空才加Head Order
            foreach(DataRow dr in orderItemRowsList){
                 ediDT.Rows.Add(dr); //  加Item
            }
        }
        // 记录问题订单号及异常信息

        string errorMessage = string.Empty;
        if(orderExceptionFieldsList.Count > 0){
           errorMessage =  "以下字段数据缺失：" + string.Join(",", orderExceptionFieldsList);
        }
        if(orderDataInvalidFieldsList.Count > 0){
            if(string.IsNullOrEmpty(errorMessage)){
                errorMessage = "以下字段数据不合法：" + string.Join(",", orderDataInvalidFieldsList);
            }else{
                errorMessage = errorMessage + " | 以下字段数据不合法："+ string.Join(",", orderDataInvalidFieldsList);
            }
        }
        // 错误信息存在
        if(!string.IsNullOrEmpty(errorMessage)){
            DataRow exceptionRow = allExceptionOrdersDT.NewRow();
            exceptionRow["order_number"] = orderNumber;
            exceptionRow["message"] =errorMessage;
            allExceptionOrdersDT.Rows.Add(exceptionRow);
        }
    }
}

// 必填字段如下，如果必填字段为空，则不进生成EDI
// Order Type  	Sales Org  	Distribution channel    	Sold to   Ship to   PO Number    Reqd Del Date  SAP Material    Qty   UoM

public DataRow buildEdiOrderRow(DataRow orderRow, ref List<string> orderExceptionFieldsList, ref List<string> orderDataInvalidFieldsList){
    DataRow ediRow = ediDT.NewRow();
    
    // 检查必填项是否为空
    string[] mandatoryFields = new string[]{ "Sold_to", "Ship_to"};
    checkFields(orderRow, mandatoryFields, ref orderExceptionFieldsList, ref orderDataInvalidFieldsList);

    ediRow["A"] = "H";
    ediRow["B"] = orderRow["Sales_Order_Type"];  // mandatory
    ediRow["C"] = orderRow["Sales_Org"]; // mandatory
    ediRow["D"] = orderRow["Distribution_channel"];   // mandatory
    if(is_edi_production){
        ediRow["E"] = orderRow["Sold_to"];  // mandatory
        ediRow["G"] = orderRow["Ship_to"];  // mandatory
    }else{
        int totalSoldToRows = soldToShipToTestDT.Rows.Count-1;
        Random r1 = new Random();
        int rowNum = r1.Next(totalSoldToRows);
        // Console.WriteLine("rowNum: {0}", rowNum);
        System.Threading.Thread.Sleep(10);
        string soldToCode = soldToShipToTestDT.Rows[rowNum]["Sold-to party"].ToString();
        string shipToCode = soldToShipToTestDT.Rows[rowNum]["Ship-to party"].ToString();
        ediRow["E"] = string.IsNullOrEmpty(orderRow["Sold_to"].ToString()) ? "" : soldToCode;  // mandatory
        ediRow["G"] = string.IsNullOrEmpty(orderRow["Ship_to"].ToString()) ? "" : shipToCode;  // mandatory
    }
    ediRow["F"] = orderRow["Sold_to_Name"];
    ediRow["H"] = orderRow["Ship_to_Name"];
    ediRow["I"] = is_Online ? orderRow["PO_Number"] : (orderRow["PO_Number"].ToString() + "-test");  // mandatory
    string Reqd_Del_Date = orderRow["Reqd_Del_Date"].ToString().Trim();
    ediRow["J"] = string.IsNullOrEmpty(Reqd_Del_Date) ? "" : Convert.ToDateTime(orderRow["Reqd_Del_Date"]).ToString("yyyyMMdd"); // mandatory
    ediRow["K"] = orderRow["Delivery_note_text"];
    ediRow["L"] = orderRow["Language"];
    ediRow["M"] = orderRow["Order_Reason"];
    ediRow["N"] = orderRow["Cost_Center"];
    ediRow["O"] = orderRow["OTC_Name"];
    ediRow["P"] = orderRow["OTC_Street"];
    ediRow["Q"] = orderRow["OTC_City_Name"];
    ediRow["R"] = orderRow["OTC_Check"];
    ediRow["S"] = orderRow["Spec_stock_partner"];
    ediRow["T"] = orderRow["Header_Assignment"];
    ediRow["U"] = orderRow["Header_Reference"];
    return ediRow;
}

public DataRow buildEdiOrderItemRow(DataRow orderRow, ref List<string> orderExceptionFieldsList, ref List<string> orderDataInvalidFieldsList, ref bool intoEdi){
    DataRow ediRow = ediDT.NewRow();
    
     // 检查必填项是否为空
    // Qty为空或者0，则此条Item不进EDI
    bool qtyInValid = emptyOrZero(orderRow["Qty"]);
    if(qtyInValid){
         intoEdi = false;
    }
    string[] mandatoryFields = new string[]{"SAP_Material"};
    checkFields(orderRow, mandatoryFields, ref orderExceptionFieldsList, ref orderDataInvalidFieldsList);

    ediRow["A"] = "L";
    if(is_edi_production){
        ediRow["B"] = orderRow["SAP_Material"];  // mandatory
    }else{
        int totalSoldToRows = materialCodeTestDT.Rows.Count - 1;
        Random r1 = new Random();
        int rowNum = r1.Next(totalSoldToRows);
        System.Threading.Thread.Sleep(10);
        string materialCode = materialCodeTestDT.Rows[rowNum]["Material"].ToString();
        ediRow["B"] = string.IsNullOrEmpty(orderRow["SAP_Material"].ToString()) ? "" : materialCode;  // mandatory 
    }
    ediRow["C"] = orderRow["SAP_Description"];
    ediRow["D"] = orderRow["Qty"];   // mandatory
    ediRow["E"] = orderRow["UoM"];   // mandatory
    ediRow["F"] = orderRow["SLoc"];
    ediRow["G"] = orderRow["Batch"];
    ediRow["H"] = orderRow["Plant"];
    ediRow["I"] = orderRow["WBS"];
    ediRow["J"] = orderRow["Item_Category"];
    ediRow["K"] = orderRow["Route"];
    ediRow["L"] = orderRow["Item_Condition_Type"];
    ediRow["M"] = orderRow["Item_Condition_Value"];
    ediRow["N"] = orderRow["Item_Condition_Type_1"];
    ediRow["O"] = orderRow["Item_Condition_Value_1"];
    return ediRow;
}

// edi DataTable 写入文件，返回文件路径
public string ediDataIntoFile(string ediFilePath){
    
    System.Text.StringBuilder sb = new System.Text.StringBuilder();
    // 遍历数据表，拼凑成字符串
    if(ediDT!=null && ediDT.Rows.Count > 0){
        int rowIndex = 0;
        foreach(DataRow drow in ediDT.Rows){
            object[] dRowArr = drow.ItemArray;
            int lastNotNullIndex = dRowArr.Length - 1;
            for(int i=lastNotNullIndex; i>=0; i--){
                if(!string.IsNullOrEmpty(dRowArr[i].ToString().Trim())){
                   lastNotNullIndex = i;
                    break;
                }
            }
            List<object> itemListWithSemiComma = drow.ItemArray.Take(lastNotNullIndex+1).ToList();
            
            List<string> newItemList = new List<string> { };
            foreach (object item in itemListWithSemiComma)
            {
                string newItem = item.ToString().Replace(";", ",");  // Replace 分号 为 逗号
                newItemList.Add(newItem);
            }
    
            string line = string.Join(";", newItemList); // CSV 以分号分隔
            sb.Append(line);
            rowIndex ++;
            if(rowIndex != ediDT.Rows.Count){
              // sb.Append(Environment.NewLine);  
                sb.Append("\n");
            }
        }
       System.Text.UTF8Encoding utf8BOM = new System.Text.UTF8Encoding(true);
       System.IO.File.WriteAllText(ediFilePath, sb.ToString().Trim(), utf8BOM); // 字符串写入文件
       return ediFilePath;
    }
    else{
        return string.Empty;
    }
}

public bool checkFileStatus(string ediFileSentPath){
    // while 文件存在, 等待5分钟
    int totalSeconds = 300;
    int passedSeonds = 0;
    bool hasFileSent = false;
    while(passedSeonds <= totalSeconds){
        if(!string.IsNullOrEmpty(ediFileSentPath) && File.Exists(ediFileSentPath)){
            hasFileSent = true;
            break;
        }else{
            System.Threading.Thread.Sleep(2000); // Sleep 2 S
            Console.WriteLine("{0} Seconds Passed, total: {1}", passedSeonds, totalSeconds);
            passedSeonds +=2;
        }
    }
    return hasFileSent;
}

public bool emptyOrZero(object value){
    string valueStr = Convert.ToString(value).Trim();
    
    if(string.IsNullOrWhiteSpace(valueStr) || valueStr == "0"){
        return true;
    }
    return false;
}

public void checkFields(DataRow orderRow, string[] mandatoryFields, ref List<string> orderExceptionFieldsList, ref List<string> orderDataInvalidFieldsList){
    foreach(string field in mandatoryFields){
        string fieldValue = orderRow[field].ToString().Trim();
        if(string.IsNullOrEmpty(fieldValue)){  // 值为空
            orderExceptionFieldsList.Add(field);
        }
        else{
            if(field == "Reqd_Del_Date"){
                bool isDateValid = verifyDate(orderRow["Reqd_Del_Date"]);
                if(!isDateValid){
                    orderDataInvalidFieldsList.Add("Reqd_Del_Date");
                }
            }else{
                bool isFieldValid = validateCharDigits(fieldValue);
                if(!isFieldValid){
                    orderDataInvalidFieldsList.Add(field);
                }
            }
        }
    }
}

public bool validateCharDigits(string item){
    Regex 数字字母中横线正则 = new Regex(@"^[a-zA-Z0-9-]+$");
    Match matchResult = 数字字母中横线正则.Match(item);
    string matchedValue = matchResult.Value;
    return !string.IsNullOrEmpty(matchedValue);
}
public bool verifyDate(object dateObj){
    try{
        Convert.ToDateTime(dateObj);
        return true;
    }catch(Exception e){
        return false;
        Console.WriteLine(e.Message);
    }
}