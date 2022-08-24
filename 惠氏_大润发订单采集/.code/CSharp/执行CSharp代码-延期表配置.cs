//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    string orderCat = "延期表";
    string 邮件接收人字段 = orderCat + "邮件接收人";
    string 邮件抄送人字段 = orderCat + "邮件抄送人";

    string mailToAddress = "";
    string mailCcAddress = "";

    checkMail(mailSettingDT, orderCat, customer_name, ref mailToAddress, ref mailCcAddress);
    dtRow_ProjectSettings[邮件接收人字段] = mailToAddress;
    dtRow_ProjectSettings[邮件抄送人字段] = mailCcAddress;
}
//在这里编写您的函数或者类

public void checkMail(DataTable mailSettingDT, string orderCategory, string customerName, ref string mailToAddress, ref string mailCcAddress){
    // Exception Order
    DataRow[] resultOrderRows= mailSettingDT.Select(String.Format("order_category = '{0}' and customer_name='{1}'", orderCategory, customerName));
    if(resultOrderRows.Length > 0){
        mailToAddress = resultOrderRows[0]["mail_receipt_address"].ToString();
        mailCcAddress = resultOrderRows[0]["mail_cc_address"].ToString();
        string[] exceptionOReceiptAddressArr = mailToAddress.Split(new string[]{";"}, StringSplitOptions.RemoveEmptyEntries);
        if(exceptionOReceiptAddressArr.Length == 0){
            errorMessageList.Add(string.Format("({0}) 邮件收件人不合法, 多个邮箱需要用英文分号(;)分隔。<br/>请在低代平台Mail Setting模块维护此信息", orderCategory));
        }else{
            mailToAddress = String.Join(";", exceptionOReceiptAddressArr);  // Exception Order Mail receiver
        }
        
    }else if(resultOrderRows.Length == 0){
        errorMessageList.Add(string.Format("{0}_{1}【{2}】 邮件收件人不存在!  请在低代平台Mail Setting模块维护此信息", customer_name, flow_name, orderCategory));
    }
}