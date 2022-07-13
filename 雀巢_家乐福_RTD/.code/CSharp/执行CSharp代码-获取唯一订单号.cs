//代码执行入口，请勿修改或删除
public void Run()
{
    //在这里编写您的代码
    增量订单单号数据表 = 增量订单数据表.DefaultView.ToTable(true, new string[]{"order_number"});
}
//在这里编写您的函数或者类