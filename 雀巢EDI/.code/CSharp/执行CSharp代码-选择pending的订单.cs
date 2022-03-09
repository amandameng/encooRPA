//代码执行入口，请勿修改或删除
public void Run()
{
   上传文件符合要求 = verifyDataFormat();
    if(!上传文件符合要求){
        return;
    }
    // 如果上传的pending文件中的订单不在 pending order池子里，则移除
   // List<string> notMatchedOrders = new List<string>{};
    for(int i=allToDoEx2oDT.Rows.Count-1; i>=0; i-- ){
        DataRow dr = allToDoEx2oDT.Rows[i];
        string orderNumber = dr["Customer_Order_Number"].ToString();
        DataRow[] drs = pendingJobDT.Select(string.Format("order_number='{0}' and edi_sent={1}", orderNumber, -1));
        if(drs.Length == 0){
            allToDoEx2oDT.Rows.Remove(dr);
            if(!notMatchedOrders.Contains(orderNumber)) notMatchedOrders.Add(orderNumber);
        }
    }
 //Convert.ToInt16("2s");   
}
//在这里编写您的函数或者类

// 验证上传的文件是否符合要求
public bool  verifyDataFormat(){
    string[] mandatoryFields = new string[]{ "Sales_Order_Type",  "Sales_Org", "Distribution_channel", "Sold_to", "Ship_to", "PO_Number", "Reqd_Del_Date", "SAP_Material",  "Qty", "UoM"};
    bool isValidFile = true;
    foreach(string colName in mandatoryFields){
        // 表头必填字段对应不上，抛错
        if(!allToDoEx2oDT.Columns.Contains(colName)){
            isValidFile = false;
            break;
        }
    }
    return isValidFile;
}