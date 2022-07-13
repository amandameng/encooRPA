//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    searchDone = true;
    foreach(DataRow dr in curOrdersDT.Rows){
       if(order_number != dr["order_number"].ToString().Trim()){
           searchDone = false;
           break;
       } 
    }
     // List<string> resultDRs = curOrdersDT.Rows.Cast<DataRow>().Select<DataRow, string>(dr => dr["order_number"].ToString()).ToList();
     //foreach(string item in )
}
//在这里编写您的函数或者类