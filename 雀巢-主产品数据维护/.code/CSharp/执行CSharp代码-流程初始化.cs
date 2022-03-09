//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    parameterCheck();
    
    
    DateTime timenow =  DateTime.Now;
    string timenowStr = timenow.ToString("yyyy-MM-dd-HH-mm-ss");
    
    string 流程工作目录 = @"C:\RPA工作目录\雀巢_沃尔玛";
    string 主产品更新文件夹 = Path.Combine(流程工作目录, @"主产品更新");  // 文件夹
    
    string 配置文件夹 = Path.Combine(流程工作目录, "配置文件");  // 文件夹
    配置文件 = Path.Combine(配置文件夹, "配置文件.xlsx");

    string 今日文件夹名称 = string.Format(timenow.ToString("yyyy-MM-dd"));
    string 当前主产品更新文件夹 = Path.Combine(主产品更新文件夹, 今日文件夹名称);

    if(!Directory.Exists(当前主产品更新文件夹)){
        Directory.CreateDirectory(当前主产品更新文件夹);
    }
    // 变量为空则赋值为当前产品路径
    if(string.IsNullOrEmpty(当前主产品文件)){
          当前主产品文件 = Path.Combine(当前主产品更新文件夹, String.Format("主产品数据_{0}.xlsx", timenowStr));  
    }

    string tmpFolder = Path.Combine(Environment.GetEnvironmentVariable("CurrentProjectSourcePath"), "Temp");
    异常数据文件 = Path.Combine(tmpFolder, "异常数据反馈.xlsx");
    
    // tmpFolder 不存在则新建
    if(!Directory.Exists(tmpFolder)){
        Directory.CreateDirectory(tmpFolder);
    }
    
    if(stagingServer == true){
        控制台资源组 =  "雀巢（中国）有限公司/应用测试";
    }else{
        控制台资源组 = "雀巢（中国）有限公司/Customer Supply Chain ";
    }
    
}
//在这里编写您的函数或者类

public void parameterCheck(){
    Console.WriteLine("stagingServer: {0}", stagingServer);
    Console.WriteLine("mailRecipient: {0}", mailRecipient);
    Console.WriteLine("主产品数据文件: {0}", 主产品数据文件);
    Console.WriteLine("本地测试: {0}", 本地测试);

    
    if(string.IsNullOrEmpty(mailRecipient)){
        流程初始化异常信息.Add("mailRecipient 不存在！流程执行后邮件将发送默认收件人");
    }
    if(string.IsNullOrEmpty(主产品数据文件)){
        流程初始化异常信息.Add("文件路径为空！执行流程时请先选择上传文件");
    }
    
}
