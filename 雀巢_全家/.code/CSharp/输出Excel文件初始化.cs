//代码执行入口，请勿修改或删除
public void Run()
{
    DateTime now = DateTime.Now;
    cleanFilePath = orderFolder + string.Format("Clean Order {0}.xlsx", now.ToString("yyyy-MM-dd-HH-mm-ss"));
    exceptionFilePath =orderFolder + string.Format("Exception Order {0}.xlsx", now.ToString("yyyy-MM-dd-HH-mm-ss"));
    excelFilePath = orderFolder + string.Format("Excel To Order {0}.xlsx", now.ToString("yyyy-MM-dd-HH-mm-ss"));
    
    //CleanTable.Columns.Remove("邮件发送日期");
    // 一下两列入数据库，但是不如Excel 报表文件
    ExcelToTable.Columns.Remove("OrderDate");
    ExcelToTable.Columns.Remove("CustomerOrderNumber");
}
//在这里编写您的函数或者类