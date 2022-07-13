//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    DataRow orderDRow =  orderDT.Rows[0];
    string orderNumber = orderDRow["order_number"].ToString();
    string document_link = orderDRow["document_link"].ToString(); //   "/Webedi2/inbound/purchaseorder/17332/45793529/7495/"
    string[] docLinkArr = document_link.Split(new string[]{"/"}, StringSplitOptions.RemoveEmptyEntries);
    string fileName = string.Format("Mbx-{0}.Type.PO.Doc-{1}.Loc-{2}.Id-{3}.en-us.pdf", docLinkArr[3], orderNumber, docLinkArr[4],docLinkArr[5]);  // Mbx-21658.Type.PO.Doc-3850505657.Loc-7466.Id-50859700.en-us.pdf
    pdfFilePath = System.IO.Path.Combine(pdfFolder, fileName); 
    Console.WriteLine(pdfFilePath);
}
//在这里编写您的函数或者类