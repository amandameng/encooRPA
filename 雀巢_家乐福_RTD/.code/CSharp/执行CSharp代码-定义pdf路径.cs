//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
   string 当前订单pdf文件模板 = GlobalVariable.VariableHelper.GetVariableValue("当前订单pdf文件模板").ToString();
   pdfFilePath = 当前订单pdf文件模板.Replace("[采购订单号]", 采购单号);
}
//在这里编写您的函数或者类