//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    foreach(DataRow dr in orderItemsMappedToWyethDT.Rows){
        string customerSkuCode = dr[curCustomerName + "编码"].ToString();
        string dcNo = dr["经销商代码"].ToString();
        string soldTo = dr["Sold To"].ToString();
        string wyethSku = dr["惠氏编码"].ToString();
        string wyethPOID = dr["客户PO"].ToString();
       
        dr["POID"] = GenerateWyethPOID(wyethPOID, soldTo, wyethSku, customerSkuCode, dcNo);
    }
    
    POIDsList = orderItemsMappedToWyethDT.Rows.Cast<DataRow>().Select<DataRow, string>(dr => dr["POID"].ToString()).ToList();
}
//在这里编写您的函数或者类

public string GenerateWyethPOID(string wyethPOID, string soldTo, string wyethSku, string customerSkuCode, string dcNo){
        string comment = string.Empty;
        DataTable specialListDT = (DataTable)dtRow_ModuleSettings["specialListDT"];
        DataRow[] specialDRInDC = specialListDT.Select(string.Format("sold_to='{0}' and sku_code = '{1}' and customer_sku_code='{2}' and dc_no = '{3}'", soldTo, wyethSku, customerSkuCode, dcNo));
        if(specialDRInDC.Length > 0){
            comment = specialDRInDC[0]["comment"].ToString();
        }else{
            DataRow[] specialDR = specialListDT.Select(string.Format("sold_to='{0}' and sku_code = '{1}' and customer_sku_code='{2}'", soldTo, wyethSku, customerSkuCode));
            if(specialDR.Length > 0){
                comment = specialDR[0]["comment"].ToString();
            }
        }
        
        if(!string.IsNullOrEmpty(comment)){
            string[] commentArr = comment.Split(new string[]{"/"}, StringSplitOptions.RemoveEmptyEntries);
            List<string> commentList = commentArr.ToList();
            if(commentList.Contains("整箱")){
                wyethPOID = wyethPOID + "-zx";
            }
           if(commentList.Contains("彩箱装")){
                wyethPOID = wyethPOID + "-cxz";
            }
           if(commentList.Contains("CVP")){
                wyethPOID = wyethPOID + "-CVP";
            }
        }
        return wyethPOID;
}