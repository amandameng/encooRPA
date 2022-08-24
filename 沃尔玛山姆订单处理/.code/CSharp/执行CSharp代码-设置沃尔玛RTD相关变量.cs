//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    
    wmRTD_eto_file_path = eto_file_path.Replace($"Copy of Excel To Order_{curCustomerName}", $"Copy of Excel To Order_{沃尔玛RTD客户名}");
    wmRTD_clean_order_file_path = clean_order_file_path.Replace($"Clean and Exception_{curCustomerName}", $"Clean and Exception_{沃尔玛RTD客户名}");
    wmRTD_分仓明细表文件 = 分仓明细表文件.Replace($"分仓明细表_{curCustomerName}", $"分仓明细表_{沃尔玛RTD客户名}");

    setCurShiptDT();
    setEtoConfigDT();
}
//在这里编写您的函数或者类

public void setCurShiptDT(){
    // 山姆 shiptTo 包含了山姆和山姆水
    if(沃尔玛shiptToDT != null){
        wmRTDShipToDT = 沃尔玛shiptToDT.Clone();
        DataRow[] shiptTpDrs = 沃尔玛shiptToDT.Select($"Customer_Name = '{沃尔玛RTD客户名}'");
        foreach(DataRow dr in shiptTpDrs){
            wmRTDShipToDT.ImportRow(dr);
        }
    }
}

public void setEtoConfigDT(){
    // 山姆 excel_to_order_config 包含了山姆和山姆水
    if(wmEtoConfigDT != null){
        wmRTDEtoConfigDT = wmEtoConfigDT.Clone();
        DataRow[] shiptTpDrs = wmEtoConfigDT.Select($"customer = '{沃尔玛RTD客户名}'");
        foreach(DataRow dr in shiptTpDrs){
            wmRTDEtoConfigDT.ImportRow(dr);
        }
    }
}