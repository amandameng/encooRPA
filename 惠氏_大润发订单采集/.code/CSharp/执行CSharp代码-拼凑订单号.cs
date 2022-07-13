//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    List<string> cleanOrderNumList = new List<string>{};
    List<string> exceptionOrderNumList = new List<string>{};

    DataTable uniqOrdersDT = newOrdersDT.DefaultView.ToTable(true, new string[]{"采购单号"});

    int rowIndex = 1;
    foreach(DataRow dr in uniqOrdersDT.Rows){
        string 订单号 = dr["采购单号"].ToString().Trim();
        
        if(cleanOrderList.Contains(订单号)){
            cleanOrderNumList.Add(订单号);
        }
        
        if(exceptionOrderList.Contains(订单号)){
            exceptionOrderNumList.Add(订单号);
        }
    }
    if(cleanOrderNumList.Count > 0){
        cleanOrderNumBatchStr = string.Join(",", cleanOrderNumList);
    }
    
    if(exceptionOrderNumList.Count > 0){
        exceptionOrderNumBatchStr = string.Join(",", exceptionOrderNumList);
    }
    
   // Convert.ToInt32("as");

}
//在这里编写您的函数或者类