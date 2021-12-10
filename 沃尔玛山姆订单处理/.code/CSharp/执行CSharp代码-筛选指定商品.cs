//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    jDDataTable = allDataDataTable.Clone();
    quManGuoDT = allDataDataTable.Clone();
    foreach(DataRow dr in allDataDataTable.Rows){
        string province = dr["Province"].ToString();
        string ItemDesc1 = dr["Item Desc 1"].ToString();
        if(province == "JD"){
            jDDataTable.ImportRow(dr);
        }
        if(ItemDesc1.Contains("趣满果")){
           quManGuoDT.ImportRow(dr);
        }
    }
}
//在这里编写您的函数或者类