//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    
    // pdfFolder = pdfFolder.Replace(流程名, 雀巢水单流程名);
    
    samWater_eto_file_path = eto_file_path.Replace("Copy of Excel To Order", $"Copy of Excel To Order_{山姆水客户名}");
    samWater_clean_order_file_path = clean_order_file_path.Replace("Clean and Exception", $"Clean and Exception_{山姆水客户名}");
    
    if(File.Exists(samWater_clean_order_file_path)){
        File.Move(samWater_clean_order_file_path, Path.Combine(Path.GetDirectoryName(samWater_clean_order_file_path), Path.GetFileNameWithoutExtension(samWater_clean_order_file_path) + DateTime.Now.ToString("-HH-mm-ss") + Path.GetExtension(samWater_clean_order_file_path)));
    }
    setEtoConfigDT();
}
//在这里编写您的函数或者类
/*
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
*/
public void setEtoConfigDT(){
    // 山姆 excel_to_order_config 包含了山姆和山姆水
    if(samEtoConfigDT != null){
        samWaterEtoConfigDT = samEtoConfigDT.Clone();
        DataRow[] shiptTpDrs = samEtoConfigDT.Select($"customer = '{山姆水客户名}'");
        foreach(DataRow dr in shiptTpDrs){
            samWaterEtoConfigDT.ImportRow(dr);
        }
    }
}