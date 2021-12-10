//代码执行入口，请勿修改或删除
public void Run()
{
    DateTime 待预约结束日期Obj = fetchvalidDate(结束日期);
    结束日期 = 待预约结束日期Obj.ToString("yyyy-MM-dd");
    List<string> allModules = new List<String>{"沃尔玛", "山姆", "沃尔玛IC"};
    string[] 沃尔玛流程模块数组 = 沃尔玛流程模块.Split(new string[]{"|"}, StringSplitOptions.RemoveEmptyEntries);
    待运行模块 = new List<String>{};
    foreach(string 模块 in 沃尔玛流程模块数组){
        if(!allModules.Contains(模块)){
            throw(new Exception(string.Format("已经实现的模块为：{0}，指定的模块未实现：{1}", string.Join("|", allModules), 模块)));
        }else{
            待运行模块.Add(模块);
        }
    }
    
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

public void mailRecipient(){
    string EX2O邮件标题 = string.Format("WM&Sam Excel to order list");
    string CleanException邮件标题 = string.Format("WM&Sam Clean + Exceptionorder list");
    string 分仓明细邮件标题 = string.Format("WM&Sam 分仓明细表");
    
    
}