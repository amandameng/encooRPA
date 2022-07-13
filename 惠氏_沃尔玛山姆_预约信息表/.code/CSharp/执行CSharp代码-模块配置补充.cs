//代码执行入口，请勿修改或删除
public string[] orderCategoryArr = new string[]{"彩箱装", "预约信息表"};
public void Run()
{
    //在这里编写您的代码
    DataTable dt_ModuleSetting = new DataTable();
        
    // dt_ModuleSetting.Columns.Add("彩箱装文件路径", typeof(string));
    // dt_ModuleSetting.Columns.Add("预约信息表文件路径", typeof(string));

    dt_ModuleSetting.Columns.Add("isWM", typeof(bool));
    dt_ModuleSetting.Columns.Add("isSam", typeof(bool));;

    dtRow_ModuleSettings = dt_ModuleSetting.NewRow(); // 外部定义
    dtRow_ModuleSettings["isWM"] = customer_name == "沃尔玛";
    dtRow_ModuleSettings["isSam"] = customer_name == "山姆";

    initExceptionFileName(ref dt_ModuleSetting);
    initRequiredDT(ref dt_ModuleSetting);

    // 邮箱数据检查
    checkMailSetting(ref dt_ModuleSetting);

    initOrderExcelRow();
    
}
//在这里编写您的函数或者类
public void initExceptionFileName(ref DataTable dt_ModuleSetting)
{
    
    DataTable orderCatResultDT = initOrderCatResultDT();
    
    foreach(string orderCat in orderCategoryArr){
        dt_ModuleSetting.Columns.Add(orderCat + "文件路径", typeof(string));
        dt_ModuleSetting.Columns.Add(orderCat + "订单PDF", typeof(string));
    }
    dt_ModuleSetting.Columns.Add("orderCatResultDT", typeof(object));
    
    dtRow_ModuleSettings["orderCatResultDT"] = orderCatResultDT; // 存放异常订单结果数据表
}

public void initRequiredDT(ref DataTable dt_ModuleSetting)
{
    dt_ModuleSetting.Columns.Add("rpaAccountsDT", typeof(object)); // rpaAccountDT
    dt_ModuleSetting.Columns.Add("mailSettingDT", typeof(object)); // mailSetting
    dt_ModuleSetting.Columns.Add("materialMasterDataDT", typeof(object)); // materialMasterDataDT
    dt_ModuleSetting.Columns.Add("soldToShipToDT", typeof(object)); // shipToSoldToDT
    dt_ModuleSetting.Columns.Add("constraintListDT", typeof(object)); // constraintListDT
    dt_ModuleSetting.Columns.Add("specialListDT", typeof(object)); // specialListDT
    

    dtRow_ModuleSettings["rpaAccountsDT"] = rpaAccountsDT;
    dtRow_ModuleSettings["mailSettingDT"] = mailSettingDT;
    dtRow_ModuleSettings["materialMasterDataDT"] = materialMasterDataDT;
    dtRow_ModuleSettings["soldToShipToDT"] = soldToShipToDT;
    dtRow_ModuleSettings["constraintListDT"] = constraintListDT;
    dtRow_ModuleSettings["specialListDT"] = specialListDT;
}

public DataTable initOrderCatResultDT(){
    DataTable orderCatResultDT = new DataTable();
    orderCatResultDT.Columns.Add("orderCat", typeof(string));
    orderCatResultDT.Columns.Add("orderCatExcelFileName", typeof(string));
    orderCatResultDT.Columns.Add("orderCatPDFFileName", typeof(string));
    orderCatResultDT.Columns.Add("mailSubject", typeof(string));

    orderCatResultDT.Columns.Add("orderCatPrintURL", typeof(string));
    orderCatResultDT.Columns.Add("orderRelatedDT", typeof(DataTable));
    
    orderCatResultDT.Rows.Add(new Object[]{ "彩箱装",  "彩箱装", "彩箱装订单附件", "彩箱装"});
    orderCatResultDT.Rows.Add(new Object[]{ "预约信息表",  "预约信息表", "预约信息表订单附件", "预约信息表"});
    
    return orderCatResultDT;
}

public void checkMailSetting(ref DataTable dt_ModuleSetting){
    if(mailSettingDT == null || mailSettingDT.Rows.Count == 0){
        errorMessageList.Add(string.Format("{0}_{1}流程 邮件接收人未设置。<br/>请在低代平台Mail Setting模块维护此信息", customer_name, flow_name));
        return;
    }

    foreach(string orderCat in orderCategoryArr){
        string 邮件接收人字段 = orderCat + "邮件接收人";
        string 邮件抄送人字段 = orderCat + "邮件抄送人";

        dt_ModuleSetting.Columns.Add(邮件接收人字段, typeof(string));
        dt_ModuleSetting.Columns.Add(邮件抄送人字段, typeof(string));
        
        string mailToAddress = "";
        string mailCcAddress = "";
    
        if((orderCat == "彩箱装" &&  customer_name == "山姆") || orderCat != "彩箱装") {
            checkMail(mailSettingDT, orderCat, customer_name, ref mailToAddress, ref mailCcAddress);
        }
        dtRow_ModuleSettings[邮件接收人字段] = mailToAddress;
        dtRow_ModuleSettings[邮件抄送人字段] = mailCcAddress;
    }
}


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

public void initOrderExcelRow(){
    DateTime now = DateTime.Parse(dtRow_ProjectSettings["指定日期"].ToString());
    string todayDateStr = now.ToString("yyyy-MM-dd");
    string todayDateStrNoDash = now.ToString("yyyyMMdd");
    string monthStr = now.ToString("yyyy-MM");

    string 项目目录 = dtRow_ProjectSettings["项目目录"].ToString();
    string 平台目录 = Path.Combine(项目目录, customer_name);

    string excelPostfix = string.Format("{0}_{1}.xlsx", customer_name, todayDateStr);
    string pdfPrefix = string.Format("{0}_{1}", customer_name, todayDateStrNoDash);

    foreach(DataRow dr in ((DataTable)dtRow_ModuleSettings["orderCatResultDT"]).Rows){
        string orderCat = dr["orderCat"].ToString();
        dtRow_ModuleSettings[orderCat + "文件路径"] = Path.Combine(平台目录, monthStr, todayDateStr, string.Format("{0}_{1}", dr["orderCatExcelFileName"].ToString(), excelPostfix));
        dtRow_ModuleSettings[orderCat + "订单PDF"] = Path.Combine(平台目录, monthStr, todayDateStr, string.Format("{0}_{1}.zip", pdfPrefix, dr["orderCatPDFFileName"].ToString()));
    }
}