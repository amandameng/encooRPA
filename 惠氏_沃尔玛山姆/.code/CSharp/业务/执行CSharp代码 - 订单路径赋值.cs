//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    if(!string.IsNullOrEmpty(pdfFilePath)){
       orderDT.Rows[0]["file_path"] = pdfFilePath.Replace("\\", "/"); // 组件bug导致需要替换
       Console.WriteLine(orderDT.Rows[0]["file_path"]);
    }
   // 订单原件.Add(pdfFilePath);
}
//在这里编写您的函数或者类