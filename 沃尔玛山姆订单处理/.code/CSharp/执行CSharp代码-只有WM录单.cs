//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    ex2O增量订单 = 增量订单数据表.Clone();
    foreach(DataRow dr in 增量订单数据表.Rows){
        string location = dr["location"].ToString();
        if(WMLocationsList.Contains(location)){
            ex2O增量订单.ImportRow(dr);
        }
    }
}
//在这里编写您的函数或者类