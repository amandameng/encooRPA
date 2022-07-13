//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    DataColumnCollection dataCols = origOrdersFromSheetDT.Columns;
    foreach(DataColumn dcol in dataCols){
        dcol.ColumnName = dcol.ColumnName.Trim();
    }
}
//在这里编写您的函数或者类