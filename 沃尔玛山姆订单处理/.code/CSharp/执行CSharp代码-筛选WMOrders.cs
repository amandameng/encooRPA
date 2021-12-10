//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    // 筛选出【customer_name】de ship_to_sold_to
    List<string> locationsList = curShipToDT.Rows.Cast<DataRow>().Select<DataRow, string>(dr => dr["Nestle_Plant_No"].ToString()).ToList();
 
    newWMORdersDT = 增量订单数据表.Clone();
    foreach(DataRow dr in 增量订单数据表.Rows){
        string location = dr["location"].ToString();
        if(locationsList.Contains(location)){
            newWMORdersDT.ImportRow(dr);
        }
    }
}
//在这里编写您的函数或者类