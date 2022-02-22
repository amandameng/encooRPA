//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    validColumnsArr = new string[]{"Document Number", "Document Type", "Received Date", "Vendor Number", "Location", "Document Link", "File Name"};
    foreach(string colName in validColumnsArr){
        if(!validOrdersFromListingDT.Columns.Contains(colName)){ // 如果数据表列不包含必须列
            validExcelFile = false;
            break;
        }
    }
}
//在这里编写您的函数或者类