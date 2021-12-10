//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    setCurShiptDT();
    setEtoConfigDT();
    
    List<string> errorMessageList = new List<string>{};

    etoMailReceiver_SamWater = checkMail(mailSettingDT, "Excel To Order", ref errorMessageList, 山姆水客户名);
    cleanExceptionOrderMailReceiver_SamWater = checkMail(mailSettingDT, "Clean And Exception Order", ref errorMessageList, 山姆水客户名);
    if(errorMessageList.Count > 0){
    throw new Exception(String.Join(", ", errorMessageList));
    }
    
}
//在这里编写您的函数或者类

    


public void setCurShiptDT(){
    // 山姆 shiptTo 包含了山姆和山姆水
    if(山姆shiptToDT != null){
        curShipToDT = 山姆shiptToDT.Clone();
        DataRow[] shiptTpDrs = 山姆shiptToDT.Select($"Customer_Name = '{curCustomerName}'");
        foreach(DataRow dr in shiptTpDrs){
            curShipToDT.ImportRow(dr);
        }
    }
}

public void setEtoConfigDT(){
    // 山姆 excel_to_order_config 包含了山姆和山姆水
    if(samEtoConfigDT != null){
        etoConfigDT = samEtoConfigDT.Clone();
        DataRow[] shiptTpDrs = samEtoConfigDT.Select($"customer = '{curCustomerName}'");
        foreach(DataRow dr in shiptTpDrs){
            etoConfigDT.ImportRow(dr);
        }
    }
}


public string checkMail(DataTable mailSettingDT, string orderCategory, ref List<string> errorMessageList, string customer_Name){
    // Exception Order
    DataRow[] resultOrderRows= mailSettingDT.Select(String.Format("Order_Category = '{0}' and Customer_Name='{1}'", orderCategory, customer_Name));
    string orderReportReceiver = String.Empty;
    if(resultOrderRows.Length > 0){
        orderReportReceiver = resultOrderRows[0]["Mail_Receipt_Address"].ToString(); // 只获取第一个地址
        string[] exceptionOReceiptAddressArr = orderReportReceiver.Split(new string[]{"/"}, StringSplitOptions.RemoveEmptyEntries);
        if(exceptionOReceiptAddressArr.Length == 0){
            errorMessageList.Add(string.Format("【{0}】({1}) mail receipient not valid, emails must split by '/'", 山姆水客户名, orderCategory));
        }else{
            orderReportReceiver = String.Join(";", exceptionOReceiptAddressArr);  // Exception Order Mail receiver
        }
        
    }else if(resultOrderRows.Length == 0){
        errorMessageList.Add(string.Format("【{0}】({1}) mail receipient does not exist!", 山姆水客户名, orderCategory));
    }
    return orderReportReceiver;
}