//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    string 平台名称 = "All";
    
    if(mailSettingDT!=null && mailSettingDT.Rows.Count > 0){
        ediMailReceiver = checkMail(mailSettingDT, orderCategoryPositive, 平台名称);
    }else{
        errorMessageList.Add(string.Format("({0}) 收件人不存在!  请在低代平台Mail Setting模块维护此信息", orderCategoryPositive));
    }
    
    exceptionEdiEmailDic = new Dictionary<string, string>();
    foreach(DataRow dr in validEdiCustomerDT.Rows){
        平台名称  = dr["customer_name"].ToString();
        if(mailSettingExceptionDT!=null && mailSettingExceptionDT.Rows.Count > 0){
            string emailReceiver = checkMail(mailSettingExceptionDT, orderCategoryException, 平台名称);
            exceptionEdiEmailDic[平台名称] = emailReceiver;
        }else{
            errorMessageList.Add(string.Format("客户：{0}的({1}) 邮件收件人不存在!  请在低代平台Mail Setting模块维护此信息", 平台名称, orderCategoryException));
        }
    }
    foreach(string item in errorMessageList){
        Console.WriteLine(item);
    }
    // Convert.ToInt16("ssd");

}
//在这里编写您的函数或者类

//在这里编写您的函数或者类
public string checkMail(DataTable mailSettingDT, string orderCategory, string customer_Name){
    // Exception Order
    DataRow[] resultOrderRows= mailSettingDT.Select(String.Format("Order_Category = '{0}' and Customer_Name='{1}'", orderCategory, customer_Name));
    string orderReportReceiver = String.Empty;
    if(resultOrderRows.Length > 0){
        DataRow dr = resultOrderRows[0];
        orderReportReceiver = dr["Mail_Receipt_Address"].ToString(); // 只获取第一个地址
        string[] exceptionOReceiptAddressArr = orderReportReceiver.Split(new string[]{"/"}, StringSplitOptions.RemoveEmptyEntries);
        if(exceptionOReceiptAddressArr.Length == 0){
            errorMessageList.Add(string.Format("({0}) 报表收件人不合法, 多个邮箱需要用'/'分隔。<br/>请在低代平台Mail Setting模块维护此信息", orderCategory));
        }else{
            orderReportReceiver = String.Join(";", exceptionOReceiptAddressArr);  // Exception Order Mail receiver
        }   
    }else if(resultOrderRows.Length == 0){
        errorMessageList.Add(string.Format("客户：{0}的({1}) 邮件收件人不存在!  请在低代平台Mail Setting模块维护此信息", customer_Name, orderCategory));
        // errorMessageList.Add(string.Format("({0}) 收件人不存在!  请在低代平台Mail Setting模块维护此信息", orderCategory));
    }
    return orderReportReceiver;
}