//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    
    //代码执行入口，请勿修改或删除

    userName = rpaAccountsDT.Rows[0]["User Name"].ToString();
    userPassword = rpaAccountsDT.Rows[0]["Password"].ToString();
    loginUrl = rpaAccountsDT.Rows[0]["Customer Login URL"].ToString();
    flowAlertEmail = rpaAccountsDT.Rows[0]["Flow Alert Receiver Email Address"].ToString();

    int daysCount = Convert.ToInt32(etoConfigDT.Rows[0]["daysCount"].ToString());
    开始日期 = DateTime.Parse(结束日期).AddDays(-(daysCount == 0 ? 7 : daysCount)).ToString("yyyy-MM-dd");

    etoMailReceiver = checkMail(mailSettingDT, "Excel To Order", curCustomerName);
    cleanExceptionOrderMailReceiver = checkMail(mailSettingDT, "Clean And Exception Order", curCustomerName);

    // 订单附件邮件接收人，only 沃尔玛ICE CREAM
    orderPdfsMailReceiver = checkMail(mailSettingDT, "订单附件", curCustomerName);
    
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