//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    // 工作目录未在【参数】中指定，或者指定的目录不存在的话，指定默认工作目录
    if(String.IsNullOrEmpty(流程工作目录) || !Directory.Exists(流程工作目录)){
        流程工作目录 = @"C:\RPA工作目录\雀巢_家乐福";  // TODO, 服务器上工作目录
    }
    
    本地订单文件夹 = Path.Combine(流程工作目录, "控制台订单文件");
    配置文件夹 = Path.Combine(流程工作目录, "配置文件");
    string 结果文件夹 = Path.Combine(流程工作目录, "结果输出");
    
    配置文件 = Path.Combine(配置文件夹, "配置文件.xlsx");
    // 配置文件不存在, 抛错指出配置文件不存在
    if(!File.Exists(配置文件)){
        throw(new Exception(String.Format("配置文件不存在：{0}", 配置文件)));
    }
    
    DateTime 待预约结束日期Obj = fetchvalidDate(结束日期);
    结束日期 = 待预约结束日期Obj.ToString("yyyy-MM-dd");
    
    DateTime timenow =  DateTime.Now;
    

    string 今日文件夹名称 = string.Format(@"{0}\{1}", 待预约结束日期Obj.ToString("yyyy-MM"), 待预约结束日期Obj.ToString("yyyy-MM-dd"));
    当前结果文件夹 = Path.Combine(结果文件夹, 今日文件夹名称);
    timenowStr = 结束日期 + timenow.ToString("-HH-mm-ss");
   // 当前结果文件夹 = Path.Combine(日结果文件夹, timenowStr);
    当前订单文件 = Path.Combine(当前结果文件夹, String.Format("雀巢_苏宁家乐福_{0}.xlsx", timenowStr));
    当前订单pdf文件模板 = Path.Combine(当前结果文件夹, String.Format("雀巢_苏宁家乐福_[采购订单号].pdf"));

    GlobalVariable.VariableHelper.SetVariableValue("客户平台", 客户平台);  // 市场全局变量组件
    GlobalVariable.VariableHelper.SetVariableValue("当前订单pdf文件模板", 当前订单pdf文件模板); // 当前订单pdf文件模板
    //templateFilesSetup(配置文件夹, timenowStr); // 模板文件相关初始化
    
    GlobalVariable.VariableHelper.SetVariableValue("是否打印订单", false); // 当前订单pdf文件模板
    
    initFolders();
}
//在这里编写您的函数或者类

public DateTime fetchvalidDate(string 参数日期){
     DateTime resultDate;
    
    if(String.IsNullOrEmpty(参数日期)){
        resultDate = DateTime.Now; // 参数不传，如果是开始时间，则今天时间 - 3, 否则是今天时间
    }else{       
        bool isValid = DateTime.TryParse(参数日期, out resultDate);
        if(!isValid){
            throw(new Exception("输入的日期格式不正确，请输入 yyyy-MM-dd格式的日期，比如：2021-10-14"));
        }
    }
    return resultDate;
}

public void initFolders(){
    if(!Directory.Exists(当前结果文件夹)){
        Directory.CreateDirectory(当前结果文件夹);
    }
}