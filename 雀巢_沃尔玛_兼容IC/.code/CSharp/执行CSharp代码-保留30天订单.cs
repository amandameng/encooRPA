//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    oneMonthData = allOrdersDT.Copy();
    int lastRowIndex = 1;
    foreach(DataRow dr in allOrdersDT.Rows){
       DateTime create_date_time = Convert.ToDateTime(dr["Received Date"]);
        if(DateTime.Compare(create_date_time, DateTime.Now.AddDays(-31)) > 0){
            oneMonthData.ImportRow(dr);
            lastRowIndex += 1;
        }
    }
    
    totalPages = lastRowIndex/100 + (lastRowIndex%100 == 0 ? 0 : 1);
}
//在这里编写您的函数或者类