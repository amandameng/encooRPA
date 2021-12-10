//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码

    string 流程工作目录 = @"C:\RPA工作目录\雀巢_沃尔玛";

    DateTime 待预约结束日期Obj = fetchvalidDate(结束日期);
    结束日期 = 待预约结束日期Obj.ToString("yyyy-MM-dd");
    
    DateTime timenow =  DateTime.Now;
    timenowStr = 结束日期 + timenow.ToString("-HH-mm-ss");
    
    string 导出文件夹 = Path.Combine(流程工作目录, "导出文件");  // 文件夹
    历史订单导出 = Path.Combine(导出文件夹, @"历史订单导出\" + 流程名);  // 文件夹
    string 结果文件夹 = Path.Combine(流程工作目录, @"结果输出\" + 流程名);  // 文件夹
    配置文件夹 = Path.Combine(流程工作目录, "配置文件");  // 文件夹
    配置文件 = Path.Combine(配置文件夹, "配置文件.xlsx");

    // 配置文件不存在, 抛错指出配置文件不存在
    if(!File.Exists(配置文件)){
        throw(new Exception(String.Format("配置文件不存在：{0}", 配置文件)));
    }

    string 今日文件夹名称 = string.Format(@"{0}\{1}", 待预约结束日期Obj.ToString("yyyy-MM"), 待预约结束日期Obj.ToString("yyyy-MM-dd"));
    当前结果文件夹 = Path.Combine(结果文件夹, 今日文件夹名称);
    string 今日导出文件夹 = Path.Combine(历史订单导出, 今日文件夹名称);
    pdfFolder = Path.Combine(导出文件夹, @"订单pdf\" + 流程名);  // 文件夹
    
    if(!Directory.Exists(当前结果文件夹)){
        Directory.CreateDirectory(当前结果文件夹);
    }
    if(!Directory.Exists(今日导出文件夹)){
        Directory.CreateDirectory(今日导出文件夹);
    }
    if(!Directory.Exists(历史订单导出)){
        Directory.CreateDirectory(历史订单导出);
    }
    if(!Directory.Exists(pdfFolder)){
        Directory.CreateDirectory(pdfFolder);
    }
    
    
    当前Listing文件 = Path.Combine(今日导出文件夹, timenowStr+"_listing.xls"); //  @"C:\RPA工作目录\雀巢_沃尔玛\导出文件\历史订单导出\雀巢沃尔玛订单\2021-11\2021-11-10\2021-11-10-16-31-25_listing.xls";
        
    // output files
    cleanAndExceptionTemplate = Path.Combine(配置文件夹, "Clean and Exception Template.xlsx");
    eX2OTemplate = Path.Combine(配置文件夹, "Copy of Excel To Order template.xlsx");
    eto_file_path = Path.Combine(当前结果文件夹, String.Format("Copy of Excel To Order_{0}.xlsx", timenowStr));
    clean_order_file_path = Path.Combine(当前结果文件夹, String.Format("Clean and Exception_{0}.xlsx", timenowStr));
    分仓明细表模板文件 = Path.Combine(配置文件夹, "分仓明细表 WM Template.xlsx");
    分仓明细表文件 = Path.Combine(当前结果文件夹, String.Format("分仓明细表_{0}.xlsx", timenowStr));

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