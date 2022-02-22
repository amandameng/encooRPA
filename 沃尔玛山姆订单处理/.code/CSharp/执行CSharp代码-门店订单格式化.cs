//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    // DataTable multiShopOrdersDTClone = multiShopOrdersDT.Copy();
    if(!multiShopOrdersDT.Columns.Contains("Date")){
        multiShopOrdersDT.Columns.Add("Date", typeof(string));
    }
    if(!multiShopOrdersDT.Columns.Contains("order_number")){
        multiShopOrdersDT.Columns.Add("order_number", typeof(string));
    }

    foreach(DataRow dr in multiShopOrdersDT.Rows){
        dr["Date"] = orderRow["Date"];
        dr["order_number"] = orderRow["order_number"];
    }
    

}
//在这里编写您的函数或者类