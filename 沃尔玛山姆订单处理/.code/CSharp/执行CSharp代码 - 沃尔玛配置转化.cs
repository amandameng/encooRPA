//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    setCurShiptDT();
    setEtoConfigDT();
    
    wmRTDetoMailReceiver = checkMail(mailSettingDT, "Excel To Order", 沃尔玛RTD客户名);
    wmRTDcleanExceptionOrderMailReceiver = checkMail(mailSettingDT, "Clean And Exception Order", 沃尔玛RTD客户名);

    // 只有山姆和沃尔玛需要分仓明细表, 山姆水和沃尔玛ICE CREAM 都不用
    //if(curCustomerName=="山姆" || curCustomerName=="沃尔玛"){
   wmRTD分仓明细MailReceiver = checkMail(mailSettingDT, "分仓明细表", 沃尔玛RTD客户名);
    
}
//在这里编写您的函数或者类

public void setCurShiptDT(){
    // 山姆 shiptTo 包含了山姆和山姆水
    if(沃尔玛shiptToDT != null){
        curShipToDT = 沃尔玛shiptToDT.Clone();
        DataRow[] shiptTpDrs = 沃尔玛shiptToDT.Select($"Customer_Name = '{curCustomerName}'");
        foreach(DataRow dr in shiptTpDrs){
            curShipToDT.ImportRow(dr);
        }
    }
}

public void setEtoConfigDT(){
    // 山姆 excel_to_order_config 包含了山姆和山姆水
    if(wmEtoConfigDT != null){
        etoConfigDT = wmEtoConfigDT.Clone();
        DataRow[] shiptTpDrs = wmEtoConfigDT.Select($"customer = '{curCustomerName}'");
        foreach(DataRow dr in shiptTpDrs){
            etoConfigDT.ImportRow(dr);
        }
    }
}

//在这里编写您的函数或者类
public string checkMail(DataTable mailSettingDT, string orderCategory, string customer_Name){
    // Exception Order
    DataRow[] resultOrderRows= mailSettingDT.Select(String.Format("Order_Category = '{0}' and Customer_Name='{1}'", orderCategory, customer_Name));
    string orderReportReceiver = String.Empty;
    if(resultOrderRows.Length > 0){
        orderReportReceiver = resultOrderRows[0]["Mail_Receipt_Address"].ToString(); // 只获取第一个地址
        string[] exceptionOReceiptAddressArr = orderReportReceiver.Split(new string[]{"/"}, StringSplitOptions.RemoveEmptyEntries);
        if(exceptionOReceiptAddressArr.Length == 0){
            errorMessageList.Add(string.Format("({0}) 报表收件人不合法, 多个邮箱需要用'/'分隔。<br/>请在低代平台Mail Setting模块维护此信息", orderCategory));
        }else{
            orderReportReceiver = String.Join(";", exceptionOReceiptAddressArr);  // Exception Order Mail receiver
        }
        
    }else if(resultOrderRows.Length == 0){
        errorMessageList.Add(string.Format("({0}) 报表收件人不存在!  请在低代平台Mail Setting模块维护此信息", orderCategory));
    }
    return orderReportReceiver;
}