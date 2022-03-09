//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    if(File.Exists(eto_file_path)){
        沃尔玛山姆报表字典["沃尔玛EX2OFile"] = eto_file_path;
    }
    if(File.Exists(clean_order_file_path)){
        沃尔玛山姆报表字典["沃尔玛CleanExceptionFile"] = clean_order_file_path;
    }
    if(File.Exists(分仓明细表文件)){
        沃尔玛山姆报表字典["沃尔玛分仓明细表"] = 分仓明细表文件;
    }
    
    if(散威化订单附件!= null && 散威化订单附件.Count > 0){
        沃尔玛山姆报表字典["沃尔玛订单pdf附件"] = 散威化订单附件; 
    }
    
    if(etoResultDT!=null && etoResultDT.Rows.Count > 0){
        沃尔玛山姆报表字典["沃尔玛EX2ODT"] = etoResultDT;
    }

    沃尔玛山姆报表字典["ex2oMailReceiver"] = etoMailReceiver;
    沃尔玛山姆报表字典["cleanExceptionMailReceiver"] = cleanExceptionOrderMailReceiver;
    沃尔玛山姆报表字典["分仓明细MailReceiver"] = 分仓明细MailReceiver;

}