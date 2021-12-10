//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    setCurShiptDT();
    setEtoConfigDT();
}
//在这里编写您的函数或者类

public void setCurShiptDT(){
    // 山姆 shiptTo 包含了山姆和山姆水
    if(山姆shiptToDT != null){
        curShipToDT = 山姆shiptToDT.Clone();
        DataRow[] shiptTpDrs = 山姆shiptToDT.Select($"Customer_Name = '{curCustomerName}'");
        foreach(DataRow dr in shiptTpDrs){
            curShipToDT.ImportRow(dr);
        }
    }
}

public void setEtoConfigDT(){
    // 山姆 excel_to_order_config 包含了山姆和山姆水
    if(samEtoConfigDT != null){
        etoConfigDT = samEtoConfigDT.Clone();
        DataRow[] shiptTpDrs = samEtoConfigDT.Select($"customer = '{curCustomerName}'");
        foreach(DataRow dr in shiptTpDrs){
            etoConfigDT.ImportRow(dr);
        }
    }
}