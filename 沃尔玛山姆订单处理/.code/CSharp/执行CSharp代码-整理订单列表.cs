//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    string receivedDate = "2/21/2021 10:04:42 PM";
    DateTime receivedDateTime = Convert.ToDateTime(receivedDate);
    Console.WriteLine(receivedDateTime);
   /* 
    foreach(DataRow dr in allOrdersDT.Rows){
        // Document Number	Document Type	Received Date	Vendor Number	Location

        string receivedDate = dr["Received Date"].ToString();
        DateTime receivedDateTime = Convert.ToDateTime(receivedDate);
    }
    
  */
}
//在这里编写您的函数或者类