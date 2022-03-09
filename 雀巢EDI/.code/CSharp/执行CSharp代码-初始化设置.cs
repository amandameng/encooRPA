//代码执行入口，请勿修改或删除
public void Run()
{
    initEdiFolder();
    //在这里编写您的代码
    currentProjectSourcePath = Environment.GetEnvironmentVariable("CurrentProjectSourcePath");

    // mail setting EDI Positive Feedback
    mailSettingEdiSQL = string.Format("select * from mail_setting where Order_Category='{0}'",  orderCategoryPositive);
    
    // mail setting EDI Exception Feedback
    mailSettingExceptionEdiSQL = string.Format("select * from mail_setting where Order_Category='{0}'",  orderCategoryException);
    
    // 邮件正文模板
    邮件正文模板成功 = @"Dear all, <br> 此次EDI执行成功，{0}，发送时间为{1} {2}";
    邮件正文模板失败 = @"Dear all, <br> 此次EDI执行失败，错误消息：{0}";
    string ediHomeFolder = @"D:\EDI";
    checkFolder(ediHomeFolder);
    timeNowTicks = DateTime.Now.Ticks;
    ex2O原始文件 = Path.Combine(ediHomeFolder, "EX2O原始文件_"+timeNowTicks+".xlsx");
}

//在这里编写您的函数或者类
public void initEdiFolder(){
    // 定义EDI data文件夹
    string theEDIHomeFolder = @"C:\Program Files\ArcESB\data";
    // 测试用假路径
    if(本地测试){
            theEDIHomeFolder = @"D:\RPA 客户\雀巢\EDI\data";
    }


    string envConnectionName = "NESTLE_TEST_AS21";
    
    if(is_edi_production){
        envConnectionName = "NESTLE_PROD_AS21";
    }
    connectionFolder = Path.Combine(theEDIHomeFolder, envConnectionName);
}

public void checkFolder(string folder){
    if(!Directory.Exists(folder)){
        Directory.CreateDirectory(folder);
    }
}