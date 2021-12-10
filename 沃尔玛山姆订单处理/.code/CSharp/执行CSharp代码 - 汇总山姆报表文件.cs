//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    if(File.Exists(eto_file_path)){
        沃尔玛山姆报表字典["山姆EX2OFile"] = eto_file_path;
    }
    if(File.Exists(clean_order_file_path)){
        沃尔玛山姆报表字典["山姆CleanExceptionFile"] = clean_order_file_path;
    }
    if(File.Exists(分仓明细表文件)){
        沃尔玛山姆报表字典["山姆分仓明细表"] = 分仓明细表文件;
    }
    
    if(File.Exists(samWater_eto_file_path)){
        沃尔玛山姆报表字典["山姆水单EX2OFile"] = samWater_eto_file_path;
    }
    
    if(File.Exists(samWater_clean_order_file_path)){
        沃尔玛山姆报表字典["山姆水单CleanExceptionFile"] = samWater_clean_order_file_path;
    }
    
    List<string> 山姆水订单List = new List<string>{};
    List<string> 山姆订单List = new List<string>{};
    
    foreach(string 订单文件 in 订单pdf附件){
       if(订单文件.Contains("SAM-IBU")){
           山姆水订单List.Add(订单文件);
       }else{
           山姆订单List.Add(订单文件);
       }
    }
    if(山姆水订单List.Count > 0){
        沃尔玛山姆报表字典["山姆水订单pdf附件"] = 山姆水订单List;
    }
    if(山姆订单List.Count > 0){
        沃尔玛山姆报表字典["山姆订单pdf附件"] = 山姆订单List;
    }

    if(!沃尔玛山姆报表字典.ContainsKey("ex2oMailReceiver")){
        沃尔玛山姆报表字典["ex2oMailReceiver"] = etoMailReceiver;
    }

    if(!沃尔玛山姆报表字典.ContainsKey("cleanExceptionMailReceiver")){
        沃尔玛山姆报表字典["cleanExceptionMailReceiver"] = cleanExceptionOrderMailReceiver;
    }

    if(!沃尔玛山姆报表字典.ContainsKey("分仓明细MailReceiver")){
        沃尔玛山姆报表字典["分仓明细MailReceiver"] = 分仓明细MailReceiver;
    }
}
//在这里编写您的函数或者类